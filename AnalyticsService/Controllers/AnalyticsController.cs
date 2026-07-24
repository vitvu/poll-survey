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

        public AnalyticsController(AnalyticsDbContext context)
        {
            _context = context;
        }

        // 1. POST: api/Analytics -> Nhận dữ liệu ghi log lượt vote từ VoteService
        [HttpPost]
        public async Task<IActionResult> AddAnalytics([FromBody] Analytics analytics)
        {
            if (string.IsNullOrWhiteSpace(analytics.PollCode))
            {
                return BadRequest(new { message = "PollCode không được rỗng." });
            }

            if (analytics.VoteTime == default)
            {
                analytics.VoteTime = DateTime.Now;
            }

            // Thêm log dữ liệu vào AnalyticsDB
            _context.Analytics.Add(analytics);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ghi nhận dữ liệu phân tích thành công." });
        }

        // 2. GET: api/Analytics/summary/abc123 -> Trả về tổng số lượt vote, option được vote nhiều nhất, và phút cao điểm
        [HttpGet("summary/{pollCode}")]
        public async Task<IActionResult> GetSummary(string pollCode)
        {
            // Lấy toàn bộ danh sách log vote của mã PollCode truyền vào
            var votes = await _context.Analytics
                .Where(a => a.PollCode == pollCode)
                .ToListAsync();

            int totalVotes = votes.Count;

            // Nếu chưa có lượt vote nào
            if (totalVotes == 0)
            {
                return Ok(new
                {
                    TotalVotes = 0,
                    TopOption = 0,
                    PeakVotingMinute = "Chưa có dữ liệu"
                });
            }

            // Tìm TopOption: Gom nhóm theo OptionId, sắp xếp giảm dần theo số lượng vote và chọn Option đứng đầu
            int topOption = votes
                .GroupBy(a => a.OptionId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            // Tìm PeakVotingMinute: Định dạng thời gian theo phút ("yyyy-MM-dd HH:mm"), gom nhóm và tìm phút có lượt vote cao nhất
            string peakVotingMinute = votes
                .GroupBy(a => a.VoteTime.ToString("yyyy-MM-dd HH:mm"))
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "Chưa có dữ liệu";

            return Ok(new
            {
                TotalVotes = totalVotes,
                TopOption = topOption,
                PeakVotingMinute = peakVotingMinute
            });
        }

        // 3. GET: api/Analytics/timeline/abc123 -> Trả về danh sách thời gian & số lượt vote phục vụ cho Chart.js ở Frontend
        [HttpGet("timeline/{pollCode}")]
        public async Task<IActionResult> GetTimeline(string pollCode)
        {
            var votes = await _context.Analytics
                .Where(a => a.PollCode == pollCode)
                .ToListAsync();

            // Gom nhóm dữ liệu theo từng khung giờ (HH:mm) để đếm số lượt vote tại thời điểm đó
            var timeline = votes
                .GroupBy(a => a.VoteTime.ToString("HH:mm"))
                .Select(g => new
                {
                    Time = g.Key,
                    Votes = g.Count()
                })
                .OrderBy(t => t.Time)
                .ToList();

            return Ok(timeline);
        }
    }
}
