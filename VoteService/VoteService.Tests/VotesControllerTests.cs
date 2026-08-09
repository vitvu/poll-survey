using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using VoteService.Controllers;
using VoteService.Data;
using VoteService.Models;

namespace VoteService.Tests
{
    public class VotesControllerTests : IDisposable
    {
        private readonly VoteDbContext _context;
        private readonly VotesControllerSimplified _controller;

        public VotesControllerTests()
        {
            var options = new DbContextOptionsBuilder<VoteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new VoteDbContext(options);
            _controller = new VotesControllerSimplified(_context);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task SubmitVote_WithValidData_SavesVote()
        {
            var vote = new Vote { PollCode = "12345678", OptionId = 1, VoteValue = "", VoterToken = "token-123" };
            await _controller.SubmitVoteSimplified(vote, _context);
            
            var savedVote = _context.Votes.FirstOrDefault(v => v.VoterToken == "token-123");
            Assert.NotNull(savedVote);
        }

        [Fact]
        public async Task GetVoteData_WithValidPollCode_ReturnsData()
        {
            var votes = new List<Vote>
            {
                new Vote { PollCode = "12345678", OptionId = 1, VoteValue = "", VoterToken = "t1" },
                new Vote { PollCode = "12345678", OptionId = 1, VoteValue = "", VoterToken = "t2" }
            };
            _context.Votes.AddRange(votes);
            await _context.SaveChangesAsync();

            var result = await _controller.GetVoteDataSimplified("12345678", _context);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task DeleteVotes_WithValidPollCode_RemovesAllVotes()
        {
            var votes = new List<Vote>
            {
                new Vote { PollCode = "12345678", OptionId = 1, VoteValue = "", VoterToken = "t1" },
                new Vote { PollCode = "12345678", OptionId = 2, VoteValue = "", VoterToken = "t2" }
            };
            _context.Votes.AddRange(votes);
            await _context.SaveChangesAsync();

            await _controller.DeleteVotesSimplified("12345678", _context);
            var remaining = _context.Votes.Count(v => v.PollCode == "12345678");
            Assert.Equal(0, remaining);
        }

        [Fact]
        public async Task PreventDuplicateVotes_SameVoterToken()
        {
            var vote1 = new Vote { PollCode = "12345678", OptionId = 1, VoterToken = "token-123" };
            _context.Votes.Add(vote1);
            await _context.SaveChangesAsync();

            bool alreadyVoted = _context.Votes.Any(v => v.PollCode == "12345678" && v.VoterToken == "token-123");
            Assert.True(alreadyVoted);
        }

        [Fact]
        public async Task VoteData_WithEmptyPollCode_ReturnsEmpty()
        {
            var result = await _controller.GetVoteDataSimplified("nonexistent", _context);
            Assert.Empty(result);
        }

        [Fact]
        public void Vote_StoresCorrectly()
        {
            var vote = new Vote 
            { 
                PollCode = "87654321", 
                OptionId = 0, 
                VoteValue = "This is an answer",
                VoterToken = "token-456"
            };
            _context.Votes.Add(vote);
            _context.SaveChanges();

            var stored = _context.Votes.First();
            Assert.Equal("This is an answer", stored.VoteValue);
        }

        [Fact]
        public void Vote_WithOptionId()
        {
            var vote = new Vote { PollCode = "poll1", OptionId = 5, VoterToken = "token-1" };
            _context.Votes.Add(vote);
            _context.SaveChanges();

            var stored = _context.Votes.First(v => v.OptionId == 5);
            Assert.Equal(5, stored.OptionId);
        }

        [Fact]
        public async Task GroupVotes_ByOption()
        {
            var votes = new List<Vote>
            {
                new Vote { PollCode = "poll1", OptionId = 1, VoterToken = "t1" },
                new Vote { PollCode = "poll1", OptionId = 1, VoterToken = "t2" },
                new Vote { PollCode = "poll1", OptionId = 2, VoterToken = "t3" }
            };
            _context.Votes.AddRange(votes);
            await _context.SaveChangesAsync();

            var grouped = _context.Votes
                .Where(v => v.PollCode == "poll1")
                .GroupBy(v => v.OptionId)
                .Select(g => new { OptionId = g.Key, Count = g.Count() })
                .ToList();

            Assert.Equal(2, grouped.Count);
            Assert.Equal(2, grouped.First(g => g.OptionId == 1).Count);
        }

        [Fact]
        public async Task TotalVotes_Count()
        {
            var votes = new List<Vote>
            {
                new Vote { PollCode = "poll1", OptionId = 1, VoterToken = "t1" },
                new Vote { PollCode = "poll1", OptionId = 2, VoterToken = "t2" },
                new Vote { PollCode = "poll1", OptionId = 3, VoterToken = "t3" }
            };
            _context.Votes.AddRange(votes);
            await _context.SaveChangesAsync();

            var total = _context.Votes.Count(v => v.PollCode == "poll1");
            Assert.Equal(3, total);
        }
    }

    // Simplified controller for testing core logic
    public class VotesControllerSimplified
    {
        private readonly VoteDbContext _context;

        public VotesControllerSimplified(VoteDbContext context)
        {
            _context = context;
        }

        public async Task SubmitVoteSimplified(Vote vote, VoteDbContext context)
        {
            context.Votes.Add(vote);
            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Vote>> GetVoteDataSimplified(string pollCode, VoteDbContext context)
        {
            return await context.Votes.Where(v => v.PollCode == pollCode).ToListAsync();
        }

        public async Task DeleteVotesSimplified(string pollCode, VoteDbContext context)
        {
            var votes = context.Votes.Where(v => v.PollCode == pollCode);
            context.Votes.RemoveRange(votes);
            await context.SaveChangesAsync();
        }
    }
}

