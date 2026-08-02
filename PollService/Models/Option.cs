namespace PollService.Models
{
    public class Option
    {
        // auto-incremented primary key from database
        public int Id { get; set; }

        // foreign key linking to parent poll
        public int PollId { get; set; }

        // display text for this option
        public string Text { get; set; } = string.Empty;
    }
}
