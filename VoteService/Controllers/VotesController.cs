using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly VoteDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<VoteHub> _hubContext;

        public VotesController(
            VoteDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHubContext<VoteHub> hubContext)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _hubContext = hubContext;
        }

        // POST: api/Votes
        [HttpPost]
        public async Task<IActionResult> SubmitVote([FromBody] Vote vote)
        {
            if (string.IsNullOrWhiteSpace(vote.PollCode))
            {
                return BadRequest(new { message = "Missing poll code." });
            }

            if (string.IsNullOrWhiteSpace(vote.VoterToken))
            {
                return BadRequest(new { message = "Missing voter token." });
            }

            if (vote.OptionId <= 0 && string.IsNullOrWhiteSpace(vote.VoteValue))
            {
                return BadRequest(new { message = "Vote must have either optionId or voteValue." });
            }

            // Check if voter already voted
            bool alreadyVoted = await _context.Votes.AnyAsync(
                v => v.PollCode == vote.PollCode && v.VoterToken == vote.VoterToken
            );

            if (alreadyVoted)
            {
                return BadRequest(new { message = "You have already voted." });
            }

            // Check if poll is still active via PollService
            var pollServiceUrl = _configuration["Services:PollServiceUrl"] ?? "http://pollservice";
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{pollServiceUrl}/api/Polls/can-vote/{vote.PollCode}");

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new { message = "Poll is closed or does not exist." });
            }

            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();

            await BroadcastVoteResults(vote.PollCode);

            return Ok(new { message = "Vote submitted successfully." });
        }

        // GET: api/Votes/12345678
        [HttpGet("{pollCode}")]
        public async Task<IActionResult> GetVoteData(string pollCode)
        {
            var votes = await _context.Votes
                .Where(vote => vote.PollCode == pollCode)
                .ToListAsync();

            var summary = votes
                .GroupBy(vote => vote.OptionId == 0 ? $"value_{vote.VoteValue}" : $"option_{vote.OptionId}")
                .Select(group => new
                {
                    optionId = group.First().OptionId,
                    voteValue = group.First().VoteValue,
                    count = group.Count()
                })
                .ToList();

            var voteDetails = votes
                .Select(vote => new
                {
                    optionId = vote.OptionId,
                    voteValue = vote.VoteValue
                })
                .ToList();

            return Ok(new
            {
                pollCode = pollCode,
                total = votes.Count,
                summary = summary,
                votes = voteDetails
            });
        }

        // DELETE: api/Votes?pollCode=12345678
        [HttpDelete]
        public async Task<IActionResult> DeleteVotes([FromQuery] string pollCode)
        {
            if (string.IsNullOrWhiteSpace(pollCode))
            {
                return BadRequest(new { message = "pollCode is required." });
            }

            var votes = await _context.Votes
                .Where(vote => vote.PollCode == pollCode)
                .ToListAsync();

            _context.Votes.RemoveRange(votes);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Votes/broadcast-closed
        [HttpPost("broadcast-closed")]
        public async Task<IActionResult> BroadcastPollClosed([FromBody] PollClosedRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PollCode))
            {
                return BadRequest(new { message = "PollCode is required." });
            }

            string groupName = $"poll_{request.PollCode}";

            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("PollClosed", new { pollCode = request.PollCode });

            return Ok();
        }

        private async Task BroadcastVoteResults(string pollCode)
        {
            var votes = await _context.Votes
                .Where(vote => vote.PollCode == pollCode)
                .ToListAsync();

            var voteResults = votes
                .GroupBy(vote => vote.OptionId == 0 ? $"value_{vote.VoteValue}" : $"option_{vote.OptionId}")
                .Select(group => new
                {
                    optionId = group.First().OptionId,
                    voteValue = group.First().VoteValue,
                    voteCount = group.Count()
                })
                .ToList();

            string groupName = $"poll_{pollCode}";

            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("VoteUpdated", new
                {
                    pollCode = pollCode,
                    totalVotes = votes.Count,
                    voteResults = voteResults
                });
        }
    }

    public class PollClosedRequest
    {
        public string PollCode { get; set; } = string.Empty;
    }
}
