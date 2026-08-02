namespace PollService.Models
{
    public class Poll
    {
        // auto-incremented primary key from database
        public int Id { get; set; }

        // unique poll code used in urls (e.g. poll123)
        public string Code { get; set; } = string.Empty;

        // the question text displayed to voters
        public string Question { get; set; } = string.Empty;

        // type of poll: multiple choice, yes no, rating, open text
        public string QuestionType { get; set; } = string.Empty;

        // current status: active or closed
        public string Status { get; set; } = "Active";

        // date and time when poll stops accepting votes
        public DateTime ExpireAt { get; set; }

        // timestamp when poll was created
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // list of voting options for this poll
        public List<Option> Options { get; set; } = new List<Option>();
    }
}
