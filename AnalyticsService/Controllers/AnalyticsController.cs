using AnalyticsService.Data;
using AnalyticsService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        // inject database context to access analytics table
        private readonly AnalyticsDbContext _databaseContext;

        public AnalyticsController(AnalyticsDbContext databaseContext)
        {
            // store database context for use in methods
            _databaseContext = databaseContext;
        }

        [HttpPost]
        public async Task<IActionResult> RecordVote([FromBody] Analytics analyticsRecord)
        {
            // check if vote time was not provided by votservice
            if (analyticsRecord.VoteTime == default(DateTime))
            {
                // set vote time to current system time if empty
                analyticsRecord.VoteTime = DateTime.Now;
            }

            // add the analytics record to database context
            _databaseContext.Analytics.Add(analyticsRecord);
            // save record to database
            await _databaseContext.SaveChangesAsync();

            // return success response
            return Ok();
        }

        [HttpGet("summary/{pollCode}")]
        public async Task<IActionResult> GetPollSummary(string pollCode)
        {
            // fetch all analytics records matching the poll code from database
            var allAnalyticsRecords = await _databaseContext.Analytics
                // filter records by poll code
                .Where(analyticsRecord => analyticsRecord.PollCode == pollCode)
                // execute query and load all results into memory
                .ToListAsync();

            // group all votes by option id
            var mostVotedOptionId = allAnalyticsRecords
                // group records by which option they voted for
                .GroupBy(analyticsRecord => analyticsRecord.OptionId)
                // sort groups by count in descending order (most votes first)
                .OrderByDescending(groupedByOption => groupedByOption.Count())
                // select only the option id from the first group
                .Select(groupedByOption => groupedByOption.Key)
                // get the first (most voted) option id or 0 if no votes
                .FirstOrDefault();

            // return response with vote statistics
            return Ok(new
            {
                // total number of votes for this poll
                totalVotes = allAnalyticsRecords.Count,
                // id of the option with most votes
                mostVotedOptionId = mostVotedOptionId
            });
        }
    }
}
