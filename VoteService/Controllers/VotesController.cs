using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using VoteService.Data;
using VoteService.Hubs;
using VoteService.Models;

namespace VoteService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VotesController : ControllerBase
    {
        // inject database context to access votes table
        private readonly VoteDbContext _databaseContext;
        // inject http client factory to call other services
        private readonly IHttpClientFactory _httpClientFactory;
        // inject configuration to read service urls
        private readonly IConfiguration _configuration;
        // inject signalr hub context to broadcast to clients
        private readonly IHubContext<VoteHub> _signalRHubContext;

        public VotesController(
            VoteDbContext databaseContext,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHubContext<VoteHub> signalRHubContext)
        {
            // store database context for use in methods
            _databaseContext = databaseContext;
            // store http client factory for use in methods
            _httpClientFactory = httpClientFactory;
            // store configuration for use in methods
            _configuration = configuration;
            // store signalr hub context for use in methods
            _signalRHubContext = signalRHubContext;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitVote([FromBody] Vote voteData)
        {
            // check if poll code is missing
            if (string.IsNullOrWhiteSpace(voteData.PollCode))
                // return 400 if poll code missing
                return BadRequest(new { message = "Missing required data." });
            
            // check if voter token is missing
            if (string.IsNullOrWhiteSpace(voteData.VoterToken))
                // return 400 if voter token missing
                return BadRequest(new { message = "Missing required data." });

            // check if voter already voted before
            bool voterAlreadyVoted = await _databaseContext.Votes
                // search for votes with same poll code and voter token
                .AnyAsync(existingVote =>
                    existingVote.PollCode == voteData.PollCode &&
                    existingVote.VoterToken == voteData.VoterToken);

            // if voter already voted return error
            if (voterAlreadyVoted)
                // return 400 if already voted
                return BadRequest(new { message = "You have already voted." });

            // create http client to call other services
            var httpClient = _httpClientFactory.CreateClient();
            // get poll service url from config or use default
            var pollServiceUrl = _configuration["Services:PollServiceUrl"] ?? "http://localhost:5248";
            // send get request to poll service to validate poll
            var pollValidationResponse = await httpClient.GetAsync(
                // build url to poll service check endpoint
                $"{pollServiceUrl}/api/Polls/check/{voteData.PollCode}"
            );

            // check if poll validation failed
            if (!pollValidationResponse.IsSuccessStatusCode)
                // return 400 if poll invalid closed or expired
                return BadRequest(new { message = "Poll is invalid or has been closed." });

            // set vote created time to current system time
            voteData.CreatedAt = DateTime.Now;
            // add vote record to database context
            _databaseContext.Votes.Add(voteData);
            // save vote to database
            await _databaseContext.SaveChangesAsync();

            // query all votes for this poll from database
            var allVotesForThisPoll = await _databaseContext.Votes
                // filter by matching poll code
                .Where(existingVote => existingVote.PollCode == voteData.PollCode)
                // group votes by option id
                .GroupBy(existingVote => existingVote.OptionId)
                // select option id and vote count for each group
                .Select(voteGroupByOption => new
                {
                    optionId = voteGroupByOption.Key,
                    voteCount = voteGroupByOption.Count()
                })
                // execute query and load results
                .ToListAsync();

            // calculate total votes for this poll
            int totalVotesForThisPoll = allVotesForThisPoll.Sum(result => result.voteCount);

            // broadcast updated results to signalr clients
            await _signalRHubContext.Clients
                // get clients in poll group
                .Group($"poll_{voteData.PollCode}")
                // send vote updated event
                .SendAsync("VoteUpdated", new
                {
                    // send poll code
                    pollCode = voteData.PollCode,
                    // send total votes
                    totalVotes = totalVotesForThisPoll,
                    // send votes per option
                    voteResults = allVotesForThisPoll
                });

            // send analytics to analytics service without waiting
            _ = SendVoteAnalyticsAsync(httpClient, voteData);

            // return success response
            return Ok(new { message = "Vote submitted successfully!" });
        }

        [HttpGet("result/{pollCode}")]
        public async Task<IActionResult> GetVoteResults(string pollCode)
        {
            // query all votes for this poll from database
            var voteResultsByOption = await _databaseContext.Votes
                // filter by matching poll code
                .Where(voteRecord => voteRecord.PollCode == pollCode)
                // group votes by option id
                .GroupBy(voteRecord => voteRecord.OptionId)
                // select option id and vote count for each group
                .Select(voteGroupByOption => new
                {
                    optionId = voteGroupByOption.Key,
                    voteCount = voteGroupByOption.Count()
                })
                // execute query and load results
                .ToListAsync();

            // return vote results
            return Ok(voteResultsByOption);
        }

        [HttpGet("total/{pollCode}")]
        public async Task<IActionResult> GetTotalVotes(string pollCode)
        {
            // count all votes for this poll
            int totalVoteCount = await _databaseContext.Votes
                // filter by matching poll code
                .CountAsync(voteRecord => voteRecord.PollCode == pollCode);

            // return total votes
            return Ok(new
            {
                // send poll code
                pollCode = pollCode,
                // send total vote count
                totalVotes = totalVoteCount
            });
        }

        [HttpGet("list/{pollCode}")]
        public async Task<IActionResult> GetVoteDetails(string pollCode)
        {
            // query all votes for this poll from database
            var voteDetailsList = await _databaseContext.Votes
                // filter by matching poll code
                .Where(voteRecord => voteRecord.PollCode == pollCode)
                // sort by creation time descending (newest first)
                .OrderByDescending(voteRecord => voteRecord.CreatedAt)
                // select specific fields
                .Select(voteRecord => new
                {
                    // option id
                    optionId = voteRecord.OptionId,
                    // vote value for open text/rating
                    voteValue = voteRecord.VoteValue,
                    // creation time
                    createdAt = voteRecord.CreatedAt
                })
                // execute query and load results
                .ToListAsync();

            // return vote details
            return Ok(voteDetailsList);
        }

        [HttpDelete("by-poll-code/{pollCode}")]
        public async Task<IActionResult> DeleteVotesByPollCode(string pollCode)
        {
            // query all votes for this poll from database
            var votesToDelete = await _databaseContext.Votes
                // filter by matching poll code
                .Where(voteRecord => voteRecord.PollCode == pollCode)
                // execute query and load results
                .ToListAsync();

            // remove all votes from database context
            _databaseContext.Votes.RemoveRange(votesToDelete);
            // save changes to database
            await _databaseContext.SaveChangesAsync();

            // return 204 no content
            return NoContent();
        }

        [HttpPost("broadcast-poll-closed")]
        public async Task<IActionResult> BroadcastPollClosed([FromBody] PollClosedRequest pollClosureRequest)
        {
            // check if poll code is missing
            if (string.IsNullOrWhiteSpace(pollClosureRequest.PollCode))
                // return 400 if poll code missing
                return BadRequest(new { message = "PollCode is required." });

            // broadcast to signalr clients
            await _signalRHubContext.Clients
                // get clients in poll group
                .Group($"poll_{pollClosureRequest.PollCode}")
                // send poll closed event
                .SendAsync("PollClosed", new
                {
                    // send poll code
                    pollCode = pollClosureRequest.PollCode,
                    // send status
                    status = "Closed"
                });

            // return success response
            return Ok(new { message = "Broadcast sent." });
        }

        private async Task SendVoteAnalyticsAsync(HttpClient httpClient, Vote voteRecord)
        {
            // wrap in try-catch to handle network errors
            try
            {
                // get analytics service url from config or use default
                var analyticsServiceUrl = _configuration["Services:AnalyticsServiceUrl"] ?? "http://localhost:5125";
                // analytics endpoint path
                const string analyticsEndpoint = "/api/Analytics";

                // create json payload with vote data
                var analyticsPayload = JsonSerializer.Serialize(new
                {
                    // poll code from vote
                    pollCode = voteRecord.PollCode,
                    // option id from vote
                    optionId = voteRecord.OptionId,
                    // vote timestamp
                    voteTime = voteRecord.CreatedAt
                });

                // send post request to analytics service
                await httpClient.PostAsync(
                    // build full url
                    $"{analyticsServiceUrl}{analyticsEndpoint}",
                    // send json payload
                    new StringContent(analyticsPayload, Encoding.UTF8, "application/json")
                );
            }
            catch (Exception exceptionMessage)
            {
                // log error but continue execution (analytics is non-critical)
                Console.WriteLine($"Warning: Failed to send analytics: {exceptionMessage.Message}");
            }
        }
    }

    // request model for broadcast-poll-closed endpoint
    public class PollClosedRequest
    {
        // poll code that was closed
        public string PollCode { get; set; } = string.Empty;
    }
}
