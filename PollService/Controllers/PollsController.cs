using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
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

        // GET /api/polls/code/{code}
        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var poll = await _context.Polls.Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.Code == code);
            return poll == null ? NotFound() : Ok(poll);
        }

        // GET /api/polls/check/{code} — VoteService calls this to validate before saving vote
        [HttpGet("check/{code}")]
        public async Task<IActionResult> Check(string code)
        {
            var poll = await _context.Polls.Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.Code == code);

            if (poll == null)        return NotFound(new { message = "Poll does not exist." });
            if (poll.Status != "Active") return BadRequest(new { message = "Poll is closed." });
            if (poll.ExpireAt <= DateTime.Now) return BadRequest(new { message = "Poll has expired." });

            return Ok(poll);
        }

        // GET /api/polls/check-option/{optionId} — VoteService calls this to validate option
        [HttpGet("check-option/{optionId:int}")]
        public async Task<IActionResult> CheckOption(int optionId)
        {
            var opt = await _context.Options.FindAsync(optionId);
            return opt == null ? NotFound() : Ok(opt);
        }

        // POST /api/polls — Create new poll
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Poll poll)
        {
            if (string.IsNullOrWhiteSpace(poll.Question))
                return BadRequest(new { message = "Question cannot be empty." });

            if (poll.ExpireAt <= DateTime.Now)
                return BadRequest(new { message = "Expiration date must be in the future." });

            if (await _context.Polls.AnyAsync(p => p.Code == poll.Code))
                return BadRequest(new { message = "Code already exists." });

            // Handle options based on question type
            poll.Options = poll.QuestionType switch
            {
                "Multiple Choice" when poll.Options?.Count >= 2 => poll.Options,
                "Multiple Choice" => throw new Exception("At least 2 options are required."),
                "Yes / No" => new List<Option> { new() { Text = "Yes" }, new() { Text = "No" } },
                _ => new List<Option>()   // Rating, Open Text
            };

            poll.CreatedAt = DateTime.Now;
            poll.Status ??= "Active";

            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByCode), new { code = poll.Code }, poll);
        }

        // PUT /api/polls/{id} — Update poll (mainly to close poll)
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Poll poll)
        {
            var existing = await _context.Polls.FindAsync(id);
            if (existing == null) return NotFound();

            var statusChanged = existing.Status != poll.Status;
            
            existing.Status   = poll.Status;
            existing.Question = poll.Question;
            existing.ExpireAt = poll.ExpireAt;

            await _context.SaveChangesAsync();

            // Broadcast poll status change via VoteService SignalR Hub
            if (statusChanged && poll.Status == "Closed")
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    var voteServiceUrl = "https://localhost:5002"; // VoteService URL
                    await client.PostAsJsonAsync($"{voteServiceUrl}/api/votes/broadcast-poll-closed", new { pollCode = existing.Code });
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the request
                    Console.WriteLine($"Failed to broadcast poll closed: {ex.Message}");
                }
            }

            return NoContent();
        }

        // DELETE /api/polls/{id} — Delete poll and all associated votes
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var poll = await _context.Polls.Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (poll == null) return NotFound();

            _context.Polls.Remove(poll);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
