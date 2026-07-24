using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using VoteService.Data;
using VoteService.Models;

namespace VoteService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotesController : ControllerBase
    {
        private readonly VoteDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Constructor tiêm phụ thuộc VoteDbContext, HttpClientFactory và Configuration
        public VotesController(VoteDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // 1. POST: api/Votes -> Thực hiện lượt bình chọn
        [HttpPost]
        public async Task<IActionResult> CreateVote([FromBody] Vote vote)
        {
            // Kiểm tra thông tin gửi lên không được rỗng
            if (string.IsNullOrWhiteSpace(vote.PollCode))
            {
                return BadRequest(new { message = "Mã cuộc bình chọn (PollCode) không được rỗng." });
            }

            if (string.IsNullOrWhiteSpace(vote.VoterToken))
            {
                return BadRequest(new { message = "Mã nhận diện (VoterToken) không được rỗng." });
            }

            // Kiểm tra nguyên tắc: Mỗi VoterToken chỉ được vote 1 lần trên cùng 1 PollCode
            bool alreadyVoted = await _context.Votes.AnyAsync(v => v.PollCode == vote.PollCode && v.VoterToken == vote.VoterToken);
            if (alreadyVoted)
            {
                return BadRequest(new { message = "Bạn đã thực hiện bình chọn cho câu hỏi này trước đó rồi." });
            }

            // Khởi tạo đối tượng HttpClient để giao tiếp giữa các service
            var client = _httpClientFactory.CreateClient();
            string pollServiceUrl = _configuration["Services:PollServiceUrl"] ?? "http://localhost:5248";
            string analyticsServiceUrl = _configuration["Services:AnalyticsServiceUrl"] ?? "http://localhost:5125";

            // Bước A: Gọi HTTP GET sang PollService để xác thực Poll (Có tồn tại? Còn hoạt động? Chưa hết hạn?)
            var pollResponse = await client.GetAsync($"{pollServiceUrl}/api/Polls/check/{vote.PollCode}");
            if (!pollResponse.IsSuccessStatusCode)
            {
                var errorDetail = await pollResponse.Content.ReadAsStringAsync();
                return BadRequest(new { message = "Poll không tồn tại, đã bị đóng hoặc hết hạn bình chọn.", detail = errorDetail });
            }

            // Bước B: Nếu có chọn OptionId (Lớn hơn 0), gọi PollService để kiểm tra Option có hợp lệ không
            if (vote.OptionId > 0)
            {
                var optionResponse = await client.GetAsync($"{pollServiceUrl}/api/Polls/check-option/{vote.OptionId}");
                if (!optionResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new { message = "Phương án lựa chọn (OptionId) không tồn tại." });
                }
            }

            // Bước C: Nếu thông tin hợp lệ -> Lưu lượt bình chọn vào cơ sở dữ liệu VoteDB
            vote.CreatedAt = DateTime.Now;
            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();

            // Bước D: Sau khi lưu thành công, gửi dữ liệu sang AnalyticsService để cập nhật báo cáo
            try
            {
                var analyticsPayload = new
                {
                    PollCode = vote.PollCode,
                    OptionId = vote.OptionId,
                    VoteTime = vote.CreatedAt
                };

                // Đóng gói JSON
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(analyticsPayload),
                    Encoding.UTF8,
                    "application/json"
                );

                // Gửi HTTP POST bất đồng bộ tới AnalyticsService
                await client.PostAsync($"{analyticsServiceUrl}/api/Analytics", jsonContent);
            }
            catch
            {
                // Bỏ qua lỗi nếu AnalyticsService tạm thời không khả dụng, không làm ảnh hưởng kết quả vote của user
            }

            return Ok(new { message = "Bình chọn thành công!", vote });
        }

        // 2. GET: api/Votes/result/abc123 -> Lấy kết quả đếm vote theo từng Option của một Poll
        [HttpGet("result/{pollCode}")]
        public async Task<IActionResult> GetResult(string pollCode)
        {
            // Gom nhóm theo OptionId và đếm số lượng vote của từng Option
            var results = await _context.Votes
                .Where(v => v.PollCode == pollCode)
                .GroupBy(v => v.OptionId)
                .Select(g => new
                {
                    OptionId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(results);
        }

        // 3. GET: api/Votes/total/abc123 -> Tính tổng số lượt vote của một Poll
        [HttpGet("total/{pollCode}")]
        public async Task<IActionResult> GetTotalVote(string pollCode)
        {
            int total = await _context.Votes.CountAsync(v => v.PollCode == pollCode);
            return Ok(new { PollCode = pollCode, TotalVotes = total });
        }
    }
}
