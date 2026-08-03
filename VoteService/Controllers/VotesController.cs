using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VoteService.Data;
using VoteService.Hubs;
using VoteService.Models;

namespace VoteService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotesController : ControllerBase
    {
        private readonly VoteDbContext _databaseContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<VoteHub> _signalRHubContext;

        public VotesController(
            VoteDbContext databaseContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHubContext<VoteHub> signalRHubContext)
        {
            _databaseContext = databaseContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _signalRHubContext = signalRHubContext;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitVote([FromBody] Vote voteData)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(voteData.PollCode) || string.IsNullOrWhiteSpace(voteData.VoterToken))
                return BadRequest(new { message = "Missing required data." });

            // Check if this voter already voted on this poll
            bool voterAlreadyVoted = await _databaseContext.Votes.AnyAsync(vote =>
                vote.PollCode == voteData.PollCode && vote.VoterToken == voteData.VoterToken);

            if (voterAlreadyVoted)
                return BadRequest(new { message = "You have already voted." });

            // Call PollService to validate poll exists and is active
            HttpClient httpClient = _httpClientFactory.CreateClient();
            string pollServiceUrl = _configuration["Services:PollServiceUrl"] ?? "https://localhost:5001";
            HttpResponseMessage pollValidationResponse = await httpClient.GetAsync(
                $"{pollServiceUrl}/api/Polls/check/{voteData.PollCode}"
            );

            if (!pollValidationResponse.IsSuccessStatusCode)
                return BadRequest(new { message = "Poll is invalid or has been closed." });

            // Save vote to database
            voteData.CreatedAt = DateTime.UtcNow;
            _databaseContext.Votes.Add(voteData);
            await _databaseContext.SaveChangesAsync();

            // Load all votes for this poll to broadcast updated results
            List<Vote> allVotesForPoll = await _databaseContext.Votes
                .Where(vote => vote.PollCode == voteData.PollCode)
                .ToListAsync();

            // Group votes by OptionId (for Multiple Choice) or VoteValue (for Yes/No, Rating, Open Text)
            // - Multiple Choice: OptionId > 0, group by OptionId
            // - Yes/No, Rating, Open Text: OptionId = 0, group by VoteValue
            var voteResultsGrouped = allVotesForPoll
                .GroupBy(vote => vote.OptionId == 0 ? $"value_{vote.VoteValue}" : $"option_{vote.OptionId}")
                .Select(group => new
                {
                    optionId = group.First().OptionId,
                    voteValue = group.First().VoteValue,
                    voteCount = group.Count()
                })
                .ToList();

            int totalVoteCount = allVotesForPoll.Count;

            // Broadcast updated vote results to all clients in the poll room
            await _signalRHubContext.Clients
                .Group($"poll_{voteData.PollCode}")
                .SendAsync("VoteUpdated", new
                {
                    pollCode = voteData.PollCode,
                    totalVotes = totalVoteCount,
                    voteResults = voteResultsGrouped
                });

            return Ok(new { message = "Vote submitted successfully!" });
        }

        [HttpGet("{pollCode}")]
        public async Task<IActionResult> GetVoteData(string pollCode)
        {
            // Fetch all votes for the poll, ordered by newest first
            List<Vote> allVotesForPoll = await _databaseContext.Votes
                .Where(vote => vote.PollCode == pollCode)
                .OrderByDescending(vote => vote.CreatedAt)
                .ToListAsync();

            // Group votes by OptionId (Multiple Choice) or VoteValue (Yes/No, Rating, Open Text)
            var voteSummary = allVotesForPoll
                .GroupBy(vote => vote.OptionId == 0 ? $"value_{vote.VoteValue}" : $"option_{vote.OptionId}")
                .Select(group => new
                {
                    optionId = group.First().OptionId,
                    voteValue = group.First().VoteValue,
                    count = group.Count()
                })
                .ToList();

            // Map all individual votes for export or detailed analysis
            var voteDetails = allVotesForPoll
                .Select(vote => new
                {
                    optionId = vote.OptionId,
                    voteValue = vote.VoteValue,
                    createdAt = vote.CreatedAt
                })
                .ToList();

            return Ok(new
            {
                pollCode = pollCode,
                total = allVotesForPoll.Count,
                summary = voteSummary,
                votes = voteDetails
            });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteVotes([FromQuery] string pollCode)
        {
            if (string.IsNullOrWhiteSpace(pollCode))
                return BadRequest(new { message = "pollCode is required." });

            // Fetch and delete all votes for this poll
            List<Vote> votesToDelete = await _databaseContext.Votes
                .Where(vote => vote.PollCode == pollCode)
                .ToListAsync();

            _databaseContext.Votes.RemoveRange(votesToDelete);
            await _databaseContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("broadcast-closed")]
        public async Task<IActionResult> BroadcastPollClosed([FromBody] PollClosedRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PollCode))
                return BadRequest(new { message = "PollCode is required." });

            // Notify all clients in the poll room that voting has ended
            await _signalRHubContext.Clients
                .Group($"poll_{request.PollCode}")
                .SendAsync("PollClosed", new
                {
                    pollCode = request.PollCode,
                    status = "Closed"
                });

            return Ok(new { message = "Broadcast sent." });
        }
    }

    public class PollClosedRequest
    {
        public string PollCode { get; set; } = string.Empty;
    }
}
