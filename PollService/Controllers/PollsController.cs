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
        //
        {
            var pollRecord = await _databaseContext.Polls
                .Include(pollEntity => pollEntity.Options)
                .FirstOrDefaultAsync(pollEntity => pollEntity.Code == code);

            if (pollRecord == null)
                return NotFound();

            return Ok(pollRecord);
        }

        [HttpGet("check/{code}")]
        public async Task<IActionResult> ValidatePoll(string code)
        {
            var pollRecord = await _databaseContext.Polls
                .Include(pollEntity => pollEntity.Options)
                .FirstOrDefaultAsync(pollEntity => pollEntity.Code == code);

            if (pollRecord == null)
                return NotFound(new { message = "Poll does not exist." });

            if (pollRecord.Status != "Active")
                return BadRequest(new { message = "Poll is closed." });

            if (pollRecord.ExpireAt <= DateTime.UtcNow)
                return BadRequest(new { message = "Poll has expired." });

            return Ok(pollRecord);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePoll([FromBody] Poll pollData)
        {
            if (string.IsNullOrWhiteSpace(pollData.Question))
                return BadRequest(new { message = "Question cannot be empty." });

            pollData.ExpireAt = DateTime.SpecifyKind(pollData.ExpireAt, DateTimeKind.Utc);
            pollData.CreatedAt = DateTime.SpecifyKind(pollData.CreatedAt, DateTimeKind.Utc);

            if (pollData.ExpireAt <= DateTime.UtcNow)
                return BadRequest(new { message = "Expiration date must be in the future." });

            bool codeExists = await _databaseContext.Polls
                .AnyAsync(existingPoll => existingPoll.Code == pollData.Code);
            if (codeExists)
                return BadRequest(new { message = "Code already exists." });

            pollData.Options = pollData.QuestionType switch
            {
                "Multiple Choice" when pollData.Options?.Count >= 2 => pollData.Options,
                "Multiple Choice" => throw new Exception("Multiple Choice requires at least 2 options."),
                "Yes / No" => new List<Option>(),
                "Rating" => new List<Option>(),
                "Open Text" => new List<Option>(),
                _ => new List<Option>()
            };

            pollData.CreatedAt = DateTime.UtcNow;
            pollData.Status ??= "Active";

            _databaseContext.Polls.Add(pollData);
            await _databaseContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPollByCode), new { code = pollData.Code }, pollData);
        }

        [HttpPut("code/{code}")]
        public async Task<IActionResult> UpdatePoll(string code, [FromBody] Poll pollUpdateData)
        {
            var existingPoll = await _databaseContext.Polls
                .FirstOrDefaultAsync(pollEntity => pollEntity.Code == code);

            if (existingPoll == null)
                return NotFound();

            bool statusIsChanging = existingPoll.Status != pollUpdateData.Status;

            existingPoll.Status = pollUpdateData.Status;
            existingPoll.Question = pollUpdateData.Question;
            existingPoll.ExpireAt = pollUpdateData.ExpireAt;

            await _databaseContext.SaveChangesAsync();

            if (statusIsChanging && pollUpdateData.Status == "Closed")
            {
                await BroadcastPollClosedToVoteService(existingPoll.Code);
            }

            return NoContent();
        }

        [HttpDelete("code/{code}")]
        public async Task<IActionResult> DeletePoll(string code)
        {
            var pollToDelete = await _databaseContext.Polls
                .Include(pollEntity => pollEntity.Options)
                .FirstOrDefaultAsync(pollEntity => pollEntity.Code == code);

            if (pollToDelete == null)
                return NotFound();

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