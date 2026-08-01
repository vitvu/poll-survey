using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using VoteService.Data;
using VoteService.Hubs;
using VoteService.Models;

namespace VoteService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotesController : ControllerBase
    {
        private readonly VoteDbContext _context;
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;
        private readonly IHubContext<VoteHub> _hub;

        public VotesController(VoteDbContext context, IHttpClientFactory http,
            IConfiguration config, IHubContext<VoteHub> hub)
        {
            _context = context; _http = http; _config = config; _hub = hub;
        }

        // POST /api/votes
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] Vote vote)
        {
            if (string.IsNullOrWhiteSpace(vote.PollCode) || string.IsNullOrWhiteSpace(vote.VoterToken))
                return BadRequest(new { message = "Missing required data." });

            // Each voter can only vote once
            if (await _context.Votes.AnyAsync(v => v.PollCode == vote.PollCode && v.VoterToken == vote.VoterToken))
                return BadRequest(new { message = "You have already voted." });

            // Check if poll is still active
            var client  = _http.CreateClient();
            var pollUrl = _config["Services:PollServiceUrl"] ?? "http://localhost:5248";
            var check   = await client.GetAsync($"{pollUrl}/api/Polls/check/{vote.PollCode}");
            if (!check.IsSuccessStatusCode)
                return BadRequest(new { message = "Poll is invalid or has been closed." });

            vote.CreatedAt = DateTime.Now;
            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();

            // Calculate new results and broadcast via SignalR
            var results = await _context.Votes
                .Where(v => v.PollCode == vote.PollCode)
                .GroupBy(v => v.OptionId)
                .Select(g => new { optionId = g.Key, count = g.Count() })
                .ToListAsync();

            var total = results.Sum(r => r.count);

            await _hub.Clients.Group($"poll_{vote.PollCode}").SendAsync("VoteUpdated", new
            {
                pollCode = vote.PollCode,
                total,
                results
            });

            // Send to AnalyticsService (fire & forget)
            _ = NotifyAnalytics(client, vote);

            return Ok(new { message = "Vote submitted successfully!" });
        }

        // GET /api/votes/result/{pollCode}
        [HttpGet("result/{pollCode}")]
        public async Task<IActionResult> GetResult(string pollCode)
        {
            var results = await _context.Votes
                .Where(v => v.PollCode == pollCode)
                .GroupBy(v => v.OptionId)
                .Select(g => new { optionId = g.Key, count = g.Count() })
                .ToListAsync();
            return Ok(results);
        }

        // GET /api/votes/total/{pollCode}
        [HttpGet("total/{pollCode}")]
        public async Task<IActionResult> GetTotal(string pollCode)
        {
            var total = await _context.Votes.CountAsync(v => v.PollCode == pollCode);
            return Ok(new { pollCode, totalVotes = total });
        }

        // GET /api/votes/list/{pollCode} — Get open-text / rating values
        [HttpGet("list/{pollCode}")]
        public async Task<IActionResult> GetList(string pollCode)
        {
            var list = await _context.Votes
                .Where(v => v.PollCode == pollCode)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new { v.OptionId, v.VoteValue, v.CreatedAt })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/votes/broadcast-poll-closed — PollService calls this to notify poll closed
        [HttpPost("broadcast-poll-closed")]
        public async Task<IActionResult> BroadcastPollClosed([FromBody] PollClosedRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PollCode))
                return BadRequest(new { message = "PollCode is required." });

            await _hub.Clients.Group($"poll_{request.PollCode}").SendAsync("PollClosed", new
            {
                pollCode = request.PollCode,
                status = "Closed"
            });

            return Ok(new { message = "Broadcast sent." });
        }

        private async Task NotifyAnalytics(HttpClient client, Vote vote)
        {
            try
            {
                var url     = _config["Services:AnalyticsServiceUrl"] ?? "http://localhost:5125";
                var payload = JsonSerializer.Serialize(new { vote.PollCode, vote.OptionId, VoteTime = vote.CreatedAt });
                await client.PostAsync($"{url}/api/Analytics",
                    new StringContent(payload, Encoding.UTF8, "application/json"));
            }
            catch { /* silent */ }
        }
    }

    public class PollClosedRequest
    {
        public string PollCode { get; set; } = string.Empty;
    }
}
