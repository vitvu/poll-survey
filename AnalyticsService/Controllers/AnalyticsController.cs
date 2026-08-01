using AnalyticsService.Data;
using AnalyticsService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly AnalyticsDbContext _context;

        public AnalyticsController(AnalyticsDbContext context) => _context = context;

        // POST /api/analytics — VoteService sends vote logs here
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] Analytics data)
        {
            data.VoteTime = data.VoteTime == default ? DateTime.Now : data.VoteTime;
            _context.Analytics.Add(data);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET /api/analytics/summary/{pollCode}
        [HttpGet("summary/{pollCode}")]
        public async Task<IActionResult> Summary(string pollCode)
        {
            var votes = await _context.Analytics
                .Where(a => a.PollCode == pollCode)
                .ToListAsync();

            return Ok(new
            {
                totalVotes = votes.Count,
                topOption  = votes.GroupBy(a => a.OptionId)
                                  .OrderByDescending(g => g.Count())
                                  .Select(g => g.Key)
                                  .FirstOrDefault()
            });
        }
    }
}
