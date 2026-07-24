namespace VoteService.Models
{
    // Lớp đại diện cho bảng Votes trong cơ sở dữ liệu VoteDB
    public class Vote
    {
        // Khóa chính (Id tự tăng)
        public int Id { get; set; }

        // Mã PollCode liên kết với cuộc bình chọn đang vote
        public string PollCode { get; set; } = string.Empty;

        // Mã phương án được chọn (OptionId), nếu là Rating/Open Text có thể là 0
        public int OptionId { get; set; }

        // Giá trị đánh giá hoặc nội dung phản hồi (nếu là dạng Rating / Open Text)
        public string VoteValue { get; set; } = string.Empty;

        // Chuỗi Token nhận diện trình duyệt/người dùng để ngăn chặn bình chọn 2 lần
        public string VoterToken { get; set; } = string.Empty;

        // Thời điểm thực hiện lượt vote
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
