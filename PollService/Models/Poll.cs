namespace PollService.Models
{
    // Lớp đại diện cho bảng Polls trong cơ sở dữ liệu PollDB
    public class Poll
    {
        // Mã định danh duy nhất (Khóa chính - Primary Key), tự động tăng
        public int Id { get; set; }

        // Mã viết tắt của cuộc bình chọn (Ví dụ: "POLL001"), dùng để chia sẻ đường dẫn
        public string Code { get; set; } = string.Empty;

        // Nội dung câu hỏi của cuộc bình chọn
        public string Question { get; set; } = string.Empty;

        // Loại câu hỏi: "Multiple Choice" (Nhiều lựa chọn), "Yes / No", "Rating" (Đánh giá), "Open Text" (Tự do)
        public string QuestionType { get; set; } = string.Empty;

        // Trạng thái của cuộc bình chọn: "Active" (Đang hoạt động), "Closed" (Đã đóng)
        public string Status { get; set; } = "Active";

        // Thời điểm hết hạn của cuộc bình chọn
        public DateTime ExpireAt { get; set; }

        // Thời điểm tạo cuộc bình chọn (mặc định lấy thời gian hiện tại)
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Quan hệ 1-Nhiều (1 Poll chứa nhiều Option lựa chọn)
        public List<Option> Options { get; set; } = new List<Option>();
    }
}
