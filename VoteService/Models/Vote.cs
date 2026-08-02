namespace VoteService.Models
{
    public class Vote
    {
        // auto-incremented primary key from database
        public int Id { get; set; }

        // poll code that this vote belongs to
        public string PollCode { get; set; } = string.Empty;

        // id of the option that was voted for
        public int OptionId { get; set; }

        // vote value for rating or open-text responses
        public string VoteValue { get; set; } = string.Empty;

        // voter browser token to prevent duplicate voting
        public string VoterToken { get; set; } = string.Empty;

        // timestamp when vote was recorded
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
