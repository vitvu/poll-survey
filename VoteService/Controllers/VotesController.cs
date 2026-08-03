using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VoteService.Data;
using VoteService.Hubs;
using VoteService.Models;

namespace VoteService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotesController : ControllerBase
    {
        // inject database context to access votes table
        private readonly VoteDbContext _databaseContext;
        // inject http client factory to call other services
        private readonly IHttpClientFactory _httpClientFactory;
        // inject configuration to read service urls
        private readonly IConfiguration _configuration;
        // inject signalr hub context to broadcast to clients
        private readonly IHubContext<VoteHub> _signalRHubContext;

        public VotesController(
            VoteDbContext databaseContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHubContext<VoteHub> signalRHubContext)
        {
            _databaseContext = databaseContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _signalRHubContext = signalRHubContext;
        }

        // POST /api/votes — Submit a new vote
        [HttpPost]
        public async Task<IActionResult> SubmitVote([FromBody] Vote voteData)
        {
            if (string.IsNullOrWhiteSpace(voteData.PollCode) || string.IsNullOrWhiteSpace(voteData.VoterToken))
                return BadRequest(new { message = "Missing required data." });

            // Check duplicate vote
            if (await _databaseContext.Votes.AnyAsync(v =>
                v.PollCode == voteData.PollCode && v.VoterToken == voteData.VoterToken))
                return BadRequest(new { message = "You have already voted." });

            // Validate poll exists and is active
            var httpClient = _httpClientFactory.CreateClient();
            var pollServiceUrl = _configuration["Services:PollServiceUrl"] ?? "http://localhost:5248";
            var pollValidationResponse = await httpClient.GetAsync(
                $"{pollServiceUrl}/api/Polls/check/{voteData.PollCode}"
            );

            if (!pollValidationResponse.IsSuccessStatusCode)
                return BadRequest(new { message = "Poll is invalid or has been closed." });

            // Save vote
            voteData.CreatedAt = DateTime.Now;
            _databaseContext.Votes.Add(voteData);
            await _databaseContext.SaveChangesAsync();

            // Broadcast updated results via SignalR
            var voteResults = await _databaseContext.Votes
                .Where(v => v.PollCode == voteData.PollCode)
                .GroupBy(v => v.OptionId)
                .Select(g => new { optionId = g.Key, voteCount = g.Count() })
                .ToListAsync();

            var totalVotes = voteResults.Sum(r => r.voteCount);

            await _signalRHubContext.Clients
                .Group($"poll_{voteData.PollCode}")
                .SendAsync("VoteUpdated", new { pollCode = voteData.PollCode, totalVotes, voteResults });

            return Ok(new { message = "Vote submitted successfully!" });
        }

        // GET /api/votes/{pollCode} — Get all vote data for a poll
        [HttpGet("{pollCode}")]
        public async Task<IActionResult> GetVoteData(string pollCode)
        {
            var allVotes = await _databaseContext.Votes
                .Where(v => v.PollCode == pollCode)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            // Group by option for summary
            var summary = allVotes
                .GroupBy(v => v.OptionId)
                .Select(g => new { optionId = g.Key, count = g.Count() })
                .ToList();

            // Individual vote details
            var votes = allVotes
                .Select(v => new { optionId = v.OptionId, voteValue = v.VoteValue, createdAt = v.CreatedAt })
                .ToList();

            return Ok(new
            {
                pollCode,
                total = allVotes.Count,
                summary,  // [{ optionId, count }]
                votes     // [{ optionId, voteValue, createdAt }]
            });
        }

        // DELETE /api/votes?pollCode={pollCode} — Delete all votes for a poll
        [HttpDelete]
        public async Task<IActionResult> DeleteVotes([FromQuery] string pollCode)
        {
            if (string.IsNullOrWhiteSpace(pollCode))
                return BadRequest(new { message = "pollCode is required." });

            var votesToDelete = await _databaseContext.Votes
                .Where(v => v.PollCode == pollCode)
                .ToListAsync();

            _databaseContext.Votes.RemoveRange(votesToDelete);
            await _databaseContext.SaveChangesAsync();

            return NoContent();
        }

        // POST /api/votes/broadcast-closed — Broadcast poll closed event (inter-service call)
        [HttpPost("broadcast-closed")]
        public async Task<IActionResult> BroadcastPollClosed([FromBody] PollClosedRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PollCode))
                return BadRequest(new { message = "PollCode is required." });

            await _signalRHubContext.Clients
                .Group($"poll_{request.PollCode}")
                .SendAsync("PollClosed", new { pollCode = request.PollCode, status = "Closed" });

            return Ok(new { message = "Broadcast sent." });
        }

    }

    // request model for broadcast-poll-closed endpoint
    public class PollClosedRequest
    {
        // poll code that was closed
        public string PollCode { get; set; } = string.Empty;
    }
}
