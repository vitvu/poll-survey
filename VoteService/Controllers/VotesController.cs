using System.Collections.Generic;
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
        private readonly IHubContext<VoteHub> _hubContext;
        private readonly IConfiguration _config;

        public VotesController(
            VoteDbContext context,
            IHttpClientFactory httpClientFactory,
            IHubContext<VoteHub> hubContext,
            IConfiguration config)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
            _config = config;
        }

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
                return BadRequest(new { message = "Please select an option or enter your answer." });
            }

            bool hasVoterAlreadyVoted = await _context.Votes.AnyAsync(
                existingVote => existingVote.PollCode == vote.PollCode && existingVote.VoterToken == vote.VoterToken
            );

            if (hasVoterAlreadyVoted)
            {
                return BadRequest(new { message = "You have already voted." });
            }

            var pollServiceUrl = _config["PollServiceUrl"] ?? "http://poll-service";
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

        [HttpGet("{pollCode}")]
        public async Task<IActionResult> GetVoteData(string pollCode)
        {
            var allVotes = await _context.Votes
                .Where(vote => vote.PollCode == pollCode)
                .ToListAsync();

            var voteDetailsList = new List<Dictionary<string, object>>();
            foreach (var vote in allVotes)
            {
                var voteDetail = new Dictionary<string, object>
                {
                    { "optionId", vote.OptionId },
                    { "voteValue", vote.VoteValue }
                };
                voteDetailsList.Add(voteDetail);
            }

            return Ok(new
            {
                pollCode = pollCode,
                total = allVotes.Count,
                votes = voteDetailsList
            });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteVotes([FromQuery] string pollCode)
        {
            if (string.IsNullOrWhiteSpace(pollCode))
            {
                return BadRequest();
            }

            await _context.Votes
                .Where(vote => vote.PollCode == pollCode)
                .ExecuteDeleteAsync();

            return NoContent();
        }

        [HttpPost("broadcast-closed")]
        public async Task<IActionResult> BroadcastPollClosed([FromBody] PollClosedRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PollCode))
            {
                return BadRequest();
            }

            string groupName = $"poll_{request.PollCode}";

            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("PollClosed", new { pollCode = request.PollCode });

            return Ok();
        }

        private async Task BroadcastVoteResults(string pollCode)
        {
            var allVotes = await _context.Votes
                .Where(vote => vote.PollCode == pollCode)
                .ToListAsync();

            var voteResultsList = new List<Dictionary<string, object>>();

            foreach (var vote in allVotes)
            {
                // Tìm xem optionId này đã có trong danh sách kết quả chưa
                Dictionary<string, object> found = null;
                foreach (var result in voteResultsList)
                {
                    int optionId = (int)result["optionId"];
                    if (optionId == vote.OptionId)
                    {
                        found = result;
                        break;
                    }
                }

                // Nếu đã có, tăng voteCount
                if (found != null)
                {
                    int voteCount = (int)found["voteCount"];
                    found["voteCount"] = voteCount + 1;
                }
                // Nếu chưa có, thêm mới vào danh sách
                else
                {
                    var newResult = new Dictionary<string, object>
                    {
                        { "optionId", vote.OptionId },
                        { "voteCount", 1 }
                    };
                    voteResultsList.Add(newResult);
                }
            }

            string hubGroupName = $"poll_{pollCode}";

            await _hubContext.Clients
                .Group(hubGroupName)
                .SendAsync("VoteUpdated", new
                {
                    pollCode = pollCode,
                    totalVotes = allVotes.Count,
                    voteResults = voteResultsList
                });
        }
    }

    public class PollClosedRequest
    {
        public string PollCode { get; set; } = string.Empty;
    }
}
