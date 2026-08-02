namespace AnalyticsService.Models
{
    public class Analytics
    {
        // auto-incremented primary key from database
        public int Id { get; set; }

        // poll code that this vote belongs to
        public string PollCode { get; set; } = string.Empty;

        // id of the option that was voted for
        public int OptionId { get; set; }

        // timestamp when the vote was recorded
        public DateTime VoteTime { get; set; } = DateTime.Now;
    }
}
