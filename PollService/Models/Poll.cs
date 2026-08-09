namespace PollService.Models
{
    public class Poll
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;

        // 1=Multiple Choice, 2=Yes/No, 3=Rating, 4=Open Text
        public int QuestionType { get; set; } = 1;

        // 0=Active, 1=Closed
        public int Status { get; set; } = 0;

        public List<Option> Options { get; set; } = new List<Option>();
    }
}
