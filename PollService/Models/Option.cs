namespace PollService.Models
{
    // Lớp đại diện cho bảng Options trong cơ sở dữ liệu PollDB
    public class Option
    {
        // Mã định danh duy nhất của phương án lựa chọn (Khóa chính)
        public int Id { get; set; }

        // Khóa ngoại liên kết tới câu hỏi Poll (PollId)
        public int PollId { get; set; }

        // Nội dung hiển thị của phương án lựa chọn (Ví dụ: "Đồng ý", "Không đồng ý")
        public string Text { get; set; } = string.Empty;
    }
}
