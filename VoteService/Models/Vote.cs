namespace VoteService.Models
{
    public class Vote
    {
        public int Id { get; set; }
        public string PollCode { get; set; } = string.Empty;
        public int OptionId { get; set; }
        public string VoteValue { get; set; } = string.Empty;
        public string VoterToken { get; set; } = string.Empty;
    }
}
