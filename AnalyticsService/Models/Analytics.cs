namespace AnalyticsService.Models
{
    // Lớp đại diện cho bảng Analytics trong cơ sở dữ liệu AnalyticsDB
    public class Analytics
    {
        // Khóa chính (Id tự tăng)
        public int Id { get; set; }

        // Mã cuộc bình chọn
        public string PollCode { get; set; } = string.Empty;

        // Mã phương án lựa chọn được vote
        public int OptionId { get; set; }

        // Thời gian ghi nhận lượt vote
        public DateTime VoteTime { get; set; } = DateTime.Now;
    }
}
