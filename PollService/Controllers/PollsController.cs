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

        public PollsController(PollDbContext context) => _context = context;

        // GET /api/polls/code/{code}
        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var poll = await _context.Polls.Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.Code == code);
            return poll == null ? NotFound() : Ok(poll);
        }

        // GET /api/polls/check/{code} — VoteService gọi để xác thực trước khi lưu vote
        [HttpGet("check/{code}")]
        public async Task<IActionResult> Check(string code)
        {
            var poll = await _context.Polls.Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.Code == code);

            if (poll == null)        return NotFound(new { message = "Poll không tồn tại." });
            if (poll.Status != "Active") return BadRequest(new { message = "Poll đã đóng." });
            if (poll.ExpireAt <= DateTime.Now) return BadRequest(new { message = "Poll đã hết hạn." });

            return Ok(poll);
        }

        // GET /api/polls/check-option/{optionId} — VoteService gọi để xác thực option
        [HttpGet("check-option/{optionId:int}")]
        public async Task<IActionResult> CheckOption(int optionId)
        {
            var opt = await _context.Options.FindAsync(optionId);
            return opt == null ? NotFound() : Ok(opt);
        }

        // POST /api/polls — Tạo poll mới
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Poll poll)
        {
            if (string.IsNullOrWhiteSpace(poll.Question))
                return BadRequest(new { message = "Câu hỏi không được rỗng." });

            if (poll.ExpireAt <= DateTime.Now)
                return BadRequest(new { message = "Thời hạn phải lớn hơn hiện tại." });

            if (await _context.Polls.AnyAsync(p => p.Code == poll.Code))
                return BadRequest(new { message = "Mã code đã tồn tại." });

            // Xử lý options theo loại
            poll.Options = poll.QuestionType switch
            {
                "Multiple Choice" when poll.Options?.Count >= 2 => poll.Options,
                "Multiple Choice" => throw new Exception("Cần ít nhất 2 lựa chọn."),
                "Yes / No" => new List<Option> { new() { Text = "Yes" }, new() { Text = "No" } },
                _ => new List<Option>()   // Rating, Open Text
            };

            poll.CreatedAt = DateTime.Now;
            poll.Status ??= "Active";

            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByCode), new { code = poll.Code }, poll);
        }

        // PUT /api/polls/{id} — Cập nhật (chủ yếu để đóng poll)
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Poll poll)
        {
            var existing = await _context.Polls.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Status   = poll.Status;
            existing.Question = poll.Question;
            existing.ExpireAt = poll.ExpireAt;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
