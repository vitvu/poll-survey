using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private readonly PollDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public PollsController(PollDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // GET: api/Polls/code/12345678
        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetPollByCode(string code)
        {
            var poll = await _context.Polls
                .Include(poll => poll.Options)
                .FirstOrDefaultAsync(poll => poll.Code == code);

            if (poll == null)
            {
                return NotFound();
            }

            return Ok(poll);
        }

        // GET: api/Polls/can-vote/12345678
        [HttpGet("can-vote/{code}")]
        public async Task<IActionResult> CanVote(string code)
        {
            var poll = await _context.Polls.FirstOrDefaultAsync(poll => poll.Code == code);

            if (poll == null)
            {
                return NotFound(new { message = "Poll does not exist." });
            }

            if (poll.Status == 1)
            {
                return BadRequest(new { message = "Poll is closed." });
            }

            return Ok(new { canVote = true });
        }

        // POST: api/Polls
        [HttpPost]
        public async Task<IActionResult> CreatePoll([FromBody] Poll poll)
        {
            if (string.IsNullOrWhiteSpace(poll.Question))
            {
                return BadRequest(new { message = "Question cannot be empty." });
            }

            if (poll.QuestionType < 1 || poll.QuestionType > 4)
            {
                return BadRequest(new { message = "Invalid question type. Must be 1 to 4." });
            }

            if (poll.QuestionType == 1 && (poll.Options == null || poll.Options.Count < 2))
            {
                return BadRequest(new { message = "Multiple Choice requires at least 2 options." });
            }

            if (poll.QuestionType != 1)
            {
                poll.Options = new List<Option>();
            }

            poll.Code = GeneratePollCode();
            poll.Status = 0;

            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            return Created($"/api/polls/code/{poll.Code}", new { poll = poll });
        }

        // PUT: api/Polls/code/12345678
        [HttpPut("code/{code}")]
        public async Task<IActionResult> UpdatePoll(string code, [FromBody] Poll pollData)
        {
            var poll = await _context.Polls.FirstOrDefaultAsync(poll => poll.Code == code);

            if (poll == null)
            {
                return NotFound();
            }

            bool statusChanged = poll.Status != pollData.Status;

            poll.Status = pollData.Status;
            poll.Question = pollData.Question;

            await _context.SaveChangesAsync();

            if (statusChanged && pollData.Status == 1)
            {
                await NotifyPollClosed(code);
            }

            return NoContent();
        }

        // DELETE: api/Polls/code/12345678
        [HttpDelete("code/{code}")]
        public async Task<IActionResult> DeletePoll(string code)
        {
            var poll = await _context.Polls
                .Include(poll => poll.Options)
                .FirstOrDefaultAsync(poll => poll.Code == code);

            if (poll == null)
            {
                return NotFound();
            }

            _context.Polls.Remove(poll);
            await _context.SaveChangesAsync();

            await DeleteVotesFromVoteService(code);

            return NoContent();
        }

        private string GeneratePollCode()
        {
            return Random.Shared.Next(10000000, 99999999).ToString();
        }

        private async Task DeleteVotesFromVoteService(string pollCode)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                await client.DeleteAsync($"http://voteservice/api/Votes?pollCode={pollCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not delete votes for poll {pollCode}: {ex.Message}");
            }
        }

        private async Task NotifyPollClosed(string pollCode)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                await client.PostAsJsonAsync(
                    "http://voteservice/api/Votes/broadcast-closed",
                    new { pollCode = pollCode }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not notify poll closed for {pollCode}: {ex.Message}");
            }
        }
    }
}
