using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PollService.Data;
using PollService.Models;

namespace PollService.Controllers
{
    // Cấu hình Route truy cập API: api/Polls
    [Route("api/[controller]")]
    [ApiController]
    public class PollsController : ControllerBase
    {
        // Khai báo biến DbContext để làm việc với Database PollDB
        private readonly PollDbContext _context;

        // Tiêm phụ thuộc (Dependency Injection) PollDbContext từ hệ thống
        public PollsController(PollDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Polls -> Lấy danh sách tất cả các Poll kèm theo danh sách Options
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Poll>>> GetPolls()
        {
            // Include(p => p.Options) giúp tải kèm danh sách lựa chọn của Poll đó
            return await _context.Polls.Include(p => p.Options).ToListAsync();
        }

        // 2. GET: api/Polls/5 -> Lấy thông tin chi tiết một Poll theo ID
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Poll>> GetPoll(int id)
        {
            // Tìm Poll theo ID truyền vào
            var poll = await _context.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Id == id);

            // Nếu không tìm thấy thì trả về lỗi 404 Not Found
            if (poll == null)
            {
                return NotFound(new { message = "Không tìm thấy cuộc bình chọn (Poll)." });
            }

            return poll;
        }

        // 3. GET: api/Polls/code/abc123 -> Lấy thông tin Poll theo mã Code (Dùng cho đường dẫn chia sẻ)
        [HttpGet("code/{code}")]
        public async Task<ActionResult<Poll>> GetPollByCode(string code)
        {
            var poll = await _context.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Code == code);

            if (poll == null)
            {
                return NotFound(new { message = "Không tìm thấy cuộc bình chọn với mã Code này." });
            }

            return poll;
        }

        // 4. GET: api/Polls/check/abc123 -> Kiểm tra tính hợp lệ của Poll (Do VoteService gọi sang)
        [HttpGet("check/{code}")]
        public async Task<IActionResult> CheckPoll(string code)
        {
            var poll = await _context.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Code == code);

            // Kiểm tra Poll có tồn tại không
            if (poll == null)
            {
                return NotFound(new { message = "Poll không tồn tại." });
            }

            // Kiểm tra Poll có bị đóng không
            if (poll.Status != "Active")
            {
                return BadRequest(new { message = "Poll này đã bị đóng." });
            }

            // Kiểm tra Poll đã quá hạn chưa
            if (poll.ExpireAt <= DateTime.Now)
            {
                return BadRequest(new { message = "Poll này đã hết hạn bình chọn." });
            }

            // Nếu hợp lệ hết thì trả về 200 OK cùng dữ liệu Poll
            return Ok(poll);
        }

        // 5. GET: api/Polls/check-option/5 -> Kiểm tra Option có tồn tại hay không (Do VoteService gọi sang)
        [HttpGet("check-option/{optionId:int}")]
        public async Task<IActionResult> CheckOption(int optionId)
        {
            var option = await _context.Options.FirstOrDefaultAsync(o => o.Id == optionId);

            if (option == null)
            {
                return NotFound(new { message = "Lựa chọn (Option) không tồn tại." });
            }

            return Ok(option);
        }

        // 6. POST: api/Polls -> Tạo cuộc bình chọn (Poll) mới
        [HttpPost]
        public async Task<ActionResult<Poll>> CreatePoll(Poll poll)
        {
            // Kiểm tra validate: Câu hỏi không được rỗng
            if (string.IsNullOrWhiteSpace(poll.Question))
            {
                return BadRequest(new { message = "Nội dung câu hỏi không được để rỗng." });
            }

            // Kiểm tra validate: Thời gian hết hạn phải lớn hơn thời gian hiện tại
            if (poll.ExpireAt <= DateTime.Now)
            {
                return BadRequest(new { message = "Thời gian hết hạn (ExpireAt) phải lớn hơn thời điểm hiện tại." });
            }

            // Kiểm tra validate: Mã Poll (Code) không được trùng lặp trong hệ thống
            if (await _context.Polls.AnyAsync(p => p.Code == poll.Code))
            {
                return BadRequest(new { message = "Mã Poll Code đã tồn tại, vui lòng dùng mã khác." });
            }

            // Xử lý sinh/kiểm tra Option theo từng loại câu hỏi (QuestionType)
            if (poll.QuestionType == "Multiple Choice")
            {
                // Câu hỏi nhiều lựa chọn bắt buộc có ít nhất 2 option
                if (poll.Options == null || poll.Options.Count < 2)
                {
                    return BadRequest(new { message = "Loại câu hỏi Multiple Choice phải chứa ít nhất 2 phương án lựa chọn." });
                }
            }
            else if (poll.QuestionType == "Yes / No")
            {
                // Loại Yes/No tự động khởi tạo 2 option chuẩn "Yes" và "No"
                poll.Options = new List<Option>
                {
                    new Option { Text = "Yes" },
                    new Option { Text = "No" }
                };
            }
            else if (poll.QuestionType == "Rating" || poll.QuestionType == "Open Text")
            {
                // Loại đánh giá sao hoặc câu hỏi tự do thì không tạo bảng Option
                poll.Options = new List<Option>();
            }

            // Cập nhật ngày tạo và trạng thái mặc định
            poll.CreatedAt = DateTime.Now;
            if (string.IsNullOrWhiteSpace(poll.Status))
            {
                poll.Status = "Active";
            }

            // Thêm Poll vào context và lưu xuống Database SQL Server
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            // Trả về mã lỗi HTTP 201 Created cùng đường dẫn lấy thông tin vừa tạo
            return CreatedAtAction(nameof(GetPoll), new { id = poll.Id }, poll);
        }

        // 7. PUT: api/Polls/5 -> Cập nhật nội dung Poll
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePoll(int id, Poll poll)
        {
            // ID trên URL phải trùng với ID trong body
            if (id != poll.Id)
            {
                return BadRequest(new { message = "ID cuộc bình chọn không khớp." });
            }

            // Tìm thông tin Poll cũ trong database
            var existingPoll = await _context.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Id == id);
            if (existingPoll == null)
            {
                return NotFound(new { message = "Không tìm thấy cuộc bình chọn." });
            }

            if (string.IsNullOrWhiteSpace(poll.Question))
            {
                return BadRequest(new { message = "Câu hỏi không được để rỗng." });
            }

            // Cập nhật các thông tin được phép sửa
            existingPoll.Question = poll.Question;
            existingPoll.Status = poll.Status;
            existingPoll.ExpireAt = poll.ExpireAt;

            // Lưu thay đổi vào DB
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 8. DELETE: api/Polls/5 -> Xóa cuộc bình chọn
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePoll(int id)
        {
            var poll = await _context.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Id == id);
            if (poll == null)
            {
                return NotFound(new { message = "Không tìm thấy cuộc bình chọn để xóa." });
            }

            // Xóa Poll (EF Core sẽ xóa kèm các Option thuộc về Poll này)
            _context.Polls.Remove(poll);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
