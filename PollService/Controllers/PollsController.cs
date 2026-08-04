using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PollService.Data;
using PollService.Models;

namespace PollService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PollsController : ControllerBase
    {
        // inject database context to access polls and options tables
        private readonly PollDbContext _databaseContext;
        // inject http client factory to call other services
        private readonly IHttpClientFactory _httpClientFactory;

        public PollsController(PollDbContext databaseContext, IHttpClientFactory httpClientFactory)
        {
            // store database context for use in methods
            _databaseContext = databaseContext;
            // store http client factory for use in methods
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetPollByCode(string code)
        //
        {
            // query polls table for matching code
            var pollRecord = await _databaseContext.Polls
                // include related options for this poll
                .Include(pollEntity => pollEntity.Options)
                // find first poll where code matches parameter
                .FirstOrDefaultAsync(pollEntity => pollEntity.Code == code);

            // check if poll was found
            if (pollRecord == null)
                // return 404 if not found
                return NotFound();

            // return poll data with options
            return Ok(pollRecord);
        }

        [HttpGet("check/{code}")]
        public async Task<IActionResult> ValidatePoll(string code)
        {
            // query polls table for matching code
            var pollRecord = await _databaseContext.Polls
                // include related options for this poll
                .Include(pollEntity => pollEntity.Options)
                // find first poll where code matches parameter
                .FirstOrDefaultAsync(pollEntity => pollEntity.Code == code);

            // check if poll was found
            if (pollRecord == null)
                // return 404 if poll does not exist
                return NotFound(new { message = "Poll does not exist." });

            // check if poll status is active
            if (pollRecord.Status != "Active")
                // return 400 if poll is closed
                return BadRequest(new { message = "Poll is closed." });

            // check if current time is before poll expiration
            if (pollRecord.ExpireAt <= DateTime.UtcNow)
                // return 400 if poll has expired
                return BadRequest(new { message = "Poll has expired." });

            // poll is valid so return it
            return Ok(pollRecord);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePoll([FromBody] Poll pollData)
        {
            // check if question text is empty
            if (string.IsNullOrWhiteSpace(pollData.Question))
                // return 400 if question is empty
                return BadRequest(new { message = "Question cannot be empty." });

            // convert expireat to utc kind since frontend sends iso strings
            pollData.ExpireAt = DateTime.SpecifyKind(pollData.ExpireAt, DateTimeKind.Utc);
            // convert createdat to utc kind since frontend sends iso strings
            pollData.CreatedAt = DateTime.SpecifyKind(pollData.CreatedAt, DateTimeKind.Utc);

            // check if expiration date is in the future
            if (pollData.ExpireAt <= DateTime.UtcNow)
                // return 400 if expiration is not in future
                return BadRequest(new { message = "Expiration date must be in the future." });

            // check if poll code already exists in database
            bool codeExists = await _databaseContext.Polls
                // query for any poll with matching code
                .AnyAsync(existingPoll => existingPoll.Code == pollData.Code);
            // if code exists return error
            if (codeExists)
                // return 400 if code is not unique
                return BadRequest(new { message = "Code already exists." });

            // auto-generate options based on question type
            pollData.Options = pollData.QuestionType switch
            {
                // for multiple choice: use provided options if at least 2
                "Multiple Choice" when pollData.Options?.Count >= 2 => pollData.Options,
                // for multiple choice without options: throw error
                "Multiple Choice" => throw new Exception("Multiple Choice requires at least 2 options."),
                // for yes/no: no options needed, vote uses VoteValue 0 or 1
                "Yes / No" => new List<Option>(),
                // for rating: no options needed, vote uses VoteValue 1-5
                "Rating" => new List<Option>(),
                // for open text: no options needed, vote uses VoteValue for text
                "Open Text" => new List<Option>(),
                // default: empty list
                _ => new List<Option>()
            };

            // set creation timestamp to current utc time
            pollData.CreatedAt = DateTime.UtcNow;
            // set default status to active if not provided
            pollData.Status ??= "Active";

            // add new poll to database context
            _databaseContext.Polls.Add(pollData);
            // save all changes to database
            await _databaseContext.SaveChangesAsync();

            // return 201 with created poll
            return CreatedAtAction(nameof(GetPollByCode), new { code = pollData.Code }, pollData);
        }

        [HttpPut("code/{code}")]
        public async Task<IActionResult> UpdatePoll(string code, [FromBody] Poll pollUpdateData)
        {
            // find existing poll by code
            var existingPoll = await _databaseContext.Polls
                // search for poll with matching code
                .FirstOrDefaultAsync(pollEntity => pollEntity.Code == code);

            // check if poll was found
            if (existingPoll == null)
                // return 404 if poll not found
                return NotFound();

            // check if status is changing
            bool statusIsChanging = existingPoll.Status != pollUpdateData.Status;

            // update poll status
            existingPoll.Status = pollUpdateData.Status;
            // update poll question
            existingPoll.Question = pollUpdateData.Question;
            // update poll expiration
            existingPoll.ExpireAt = pollUpdateData.ExpireAt;

            // save changes to database
            await _databaseContext.SaveChangesAsync();

            // check if poll is being closed
            if (statusIsChanging && pollUpdateData.Status == "Closed")
            {
                // notify voteservice that poll is closed
                await BroadcastPollClosedToVoteService(existingPoll.Code);
            }

            // return 204 no content
            return NoContent();
        }

        [HttpDelete("code/{code}")]
        public async Task<IActionResult> DeletePoll(string code)
        {
            // find poll by code including its options
            var pollToDelete = await _databaseContext.Polls
                // include related options
                .Include(pollEntity => pollEntity.Options)
                // find poll with matching code
                .FirstOrDefaultAsync(pollEntity => pollEntity.Code == code);

            // check if poll was found
            if (pollToDelete == null)
                // return 404 if poll not found
                return NotFound();

            // remove poll from database context
            _databaseContext.Polls.Remove(pollToDelete);
            // save changes to database
            await _databaseContext.SaveChangesAsync();

            // notify voteservice to delete all votes for this poll
            await DeleteVotesFromVoteService(code);

            // return 204 no content
            return NoContent();
        }

        private async Task DeleteVotesFromVoteService(string pollCode)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                const string voteServiceUrl = "https://localhost:5002";
                await httpClient.DeleteAsync($"{voteServiceUrl}/api/Votes?pollCode={pollCode}");
            }
            catch (Exception exceptionMessage)
            {
                Console.WriteLine($"Warning: Failed to delete votes for poll {pollCode}: {exceptionMessage.Message}");
            }
        }

        private async Task BroadcastPollClosedToVoteService(string pollCode)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                const string voteServiceUrl = "https://localhost:5002";
                await httpClient.PostAsJsonAsync(
                    $"{voteServiceUrl}/api/Votes/broadcast-closed",
                    new { pollCode = pollCode }
                );
            }
            catch (Exception exceptionMessage)
            {
                Console.WriteLine($"Warning: Failed to broadcast poll closed for {pollCode}: {exceptionMessage.Message}");
            }
        }
    }
}
