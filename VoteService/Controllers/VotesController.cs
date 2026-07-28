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

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] Vote vote)
        {
            if (string.IsNullOrWhiteSpace(vote.PollCode) || string.IsNullOrWhiteSpace(vote.VoterToken))
                return BadRequest(new { message = "Thiếu dữ liệu." });

            if (await _context.Votes.AnyAsync(v => v.PollCode == vote.PollCode && v.VoterToken == vote.VoterToken))
                return BadRequest(new { message = "Bạn đã bình chọn rồi." });

            var client  = _http.CreateClient();
            var pollUrl = _config["Services:PollServiceUrl"] ?? "http://localhost:5248";
            var check   = await client.GetAsync($"{pollUrl}/api/Polls/check/{vote.PollCode}");
            if (!check.IsSuccessStatusCode)
                return BadRequest(new { message = "Poll không hợp lệ hoặc đã đóng." });

            vote.CreatedAt = DateTime.Now;
            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();

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

            _ = NotifyAnalytics(client, vote);

            return Ok(new { message = "Bình chọn thành công!" });
        }

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

        [HttpGet("total/{pollCode}")]
        public async Task<IActionResult> GetTotal(string pollCode)
        {
            var total = await _context.Votes.CountAsync(v => v.PollCode == pollCode);
            return Ok(new { pollCode, totalVotes = total });
        }

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

        private async Task NotifyAnalytics(HttpClient client, Vote vote)
        {
            try
            {
                var url     = _config["Services:AnalyticsServiceUrl"] ?? "http://localhost:5125";
                var payload = JsonSerializer.Serialize(new { vote.PollCode, vote.OptionId, VoteTime = vote.CreatedAt });
                await client.PostAsync($"{url}/api/Analytics",
                    new StringContent(payload, Encoding.UTF8, "application/json"));
            }
            catch { }
        }
    }
}
