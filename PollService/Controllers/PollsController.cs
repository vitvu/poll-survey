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
        private readonly PollDbContext _databaseContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public PollsController(PollDbContext databaseContext, IHttpClientFactory httpClientFactory)
        {
            _databaseContext = databaseContext;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetPollByCode(string code)
        {
            var pollRecord = await _databaseContext.Polls
                .Include(pollEntity => pollEntity.Options)
                .FirstOrDefaultAsync(pollEntity => pollEntity.Code == code);

            if (pollRecord == null)
            {
                return NotFound();
            }

            return Ok(pollRecord);
        }

        [HttpGet("check/{code}")]
        public async Task<IActionResult> ValidatePoll(string code)
        {
            var pollRecord = await _databaseContext.Polls
                .Include(pollEntity => pollEntity.Options)
                .FirstOrDefaultAsync(pollEntity => pollEntity.Code == code);

            if (pollRecord == null)
            {
                return NotFound(new { message = "Poll does not exist." });
            }

            if (pollRecord.Status != "Active")
            {
                return BadRequest(new { message = "Poll is closed." });
            }

            if (pollRecord.ExpireAt <= DateTime.UtcNow)
            {
                return BadRequest(new { message = "Poll has expired." });
            }

            return Ok(pollRecord);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePoll([FromBody] Poll pollData)
        {
            // Validate question is not empty
            if (string.IsNullOrWhiteSpace(pollData.Question))
            {
                return BadRequest(new { message = "Question cannot be empty." });
            }

            // Convert expireAt to UTC
            pollData.ExpireAt = DateTime.SpecifyKind(pollData.ExpireAt, DateTimeKind.Utc);
            
            // Convert createdAt to UTC
            pollData.CreatedAt = DateTime.SpecifyKind(pollData.CreatedAt, DateTimeKind.Utc);

            // Validate expiration date is in the future
            if (pollData.ExpireAt <= DateTime.UtcNow)
            {
                return BadRequest(new { message = "Expiration date must be in the future." });
            }

            // Check if poll code already exists
            bool codeExists = await _databaseContext.Polls
                .AnyAsync(existingPoll => existingPoll.Code == pollData.Code);
            
            if (codeExists)
            {
                return BadRequest(new { message = "Code already exists." });
            }

            // Auto-generate options based on question type
            if (pollData.QuestionType == "Multiple Choice")
            {
                if (pollData.Options == null || pollData.Options.Count < 2)
                {
                    return BadRequest(new { message = "Multiple Choice requires at least 2 options." });
                }
                pollData.Options = pollData.Options;
            }
            else if (pollData.QuestionType == "Yes / No")
            {
                // Yes/No doesn't need options - create empty list
                pollData.Options = new List<Option>();
            }
            else if (pollData.QuestionType == "Rating")
            {
                pollData.Options = new List<Option>();
            }
            else if (pollData.QuestionType == "Open Text")
            {
                pollData.Options = new List<Option>();
            }
            else
            {
                pollData.Options = new List<Option>();
            }

            pollData.CreatedAt = DateTime.UtcNow;
            
            if (string.IsNullOrEmpty(pollData.Status))
            {
                pollData.Status = "Active";
            }

            _databaseContext.Polls.Add(pollData);
            
            await _databaseContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPollByCode), new { code = pollData.Code }, pollData);
        }

        [HttpPut("code/{code}")]
        public async Task<IActionResult> UpdatePoll(string code, [FromBody] Poll pollUpdateData)
        {
            // Find existing poll by code
            var existingPoll = await _databaseContext.Polls
                .FirstOrDefaultAsync(pollEntity => pollEntity.Code == code);

            if (existingPoll == null)
            {
                return NotFound();
            }

            // Check if status is changing
            bool statusIsChanging = existingPoll.Status != pollUpdateData.Status;

            existingPoll.Status = pollUpdateData.Status;
            existingPoll.Question = pollUpdateData.Question;
            existingPoll.ExpireAt = pollUpdateData.ExpireAt;

            await _databaseContext.SaveChangesAsync();

            // If poll is being closed, broadcast to VoteService
            if (statusIsChanging && pollUpdateData.Status == "Closed")
            {
                await BroadcastPollClosedToVoteService(existingPoll.Code);
            }

            return NoContent();
        }

        [HttpDelete("code/{code}")]
        public async Task<IActionResult> DeletePoll(string code)
        {
            // Find poll by code with its options
            var pollToDelete = await _databaseContext.Polls
                .Include(pollEntity => pollEntity.Options)
                .FirstOrDefaultAsync(pollEntity => pollEntity.Code == code);

            if (pollToDelete == null)
            {
                return NotFound();
            }

            _databaseContext.Polls.Remove(pollToDelete);
            
            await _databaseContext.SaveChangesAsync();

            await DeleteVotesFromVoteService(code);

            return NoContent();
        }

        private async Task DeleteVotesFromVoteService(string pollCode)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                const string voteServiceUrl = "https://localhost:5002";
                
                // Call VoteService to delete votes for this poll
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
