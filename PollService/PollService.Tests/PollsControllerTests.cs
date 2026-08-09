using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using PollService.Controllers;
using PollService.Data;
using PollService.Models;

namespace PollService.Tests
{
    public class PollsControllerTests : IDisposable
    {
        private readonly PollDbContext _context;
        private readonly PollsController _controller;
        private readonly HttpClientFactoryMock _httpClientFactory;

        public PollsControllerTests()
        {
            var options = new DbContextOptionsBuilder<PollDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PollDbContext(options);
            _httpClientFactory = new HttpClientFactoryMock();
            _controller = new PollsController(_context, _httpClientFactory);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task CreatePoll_WithValidMultipleChoice_ReturnsCreatedResult()
        {
            var poll = new Poll
            {
                Question = "Favorite color?",
                QuestionType = 1,
                Options = new List<Option> { new Option { Text = "Red" }, new Option { Text = "Blue" } }
            };

            var result = await _controller.CreatePoll(poll);
            Assert.IsType<CreatedResult>(result);
        }

        [Fact]
        public async Task CreatePoll_WithEmptyQuestion_ReturnsBadRequest()
        {
            var poll = new Poll { Question = "", QuestionType = 1, Options = new List<Option>() };
            var result = await _controller.CreatePoll(poll);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreatePoll_WithInvalidQuestionType_ReturnsBadRequest()
        {
            var poll = new Poll { Question = "Test", QuestionType = 5, Options = new List<Option>() };
            var result = await _controller.CreatePoll(poll);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreatePoll_WithYesNo_ReturnsCreatedResult()
        {
            var poll = new Poll { Question = "Do you like it?", QuestionType = 2, Options = new List<Option>() };
            var result = await _controller.CreatePoll(poll);
            Assert.IsType<CreatedResult>(result);
        }

        [Fact]
        public async Task CreatePoll_WithRating_ReturnsCreatedResult()
        {
            var poll = new Poll { Question = "Rate it", QuestionType = 3, Options = new List<Option>() };
            var result = await _controller.CreatePoll(poll);
            Assert.IsType<CreatedResult>(result);
        }

        [Fact]
        public async Task CreatePoll_WithOpenText_ReturnsCreatedResult()
        {
            var poll = new Poll { Question = "Your opinion?", QuestionType = 4, Options = new List<Option>() };
            var result = await _controller.CreatePoll(poll);
            Assert.IsType<CreatedResult>(result);
        }

        [Fact]
        public async Task GetPollByCode_WithValidCode_ReturnsPoll()
        {
            var poll = new Poll { Question = "Test?", Code = "12345678", Status = 0, Options = new List<Option>() };
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var result = await _controller.GetPollByCode("12345678");
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetPollByCode_WithInvalidCode_ReturnsNotFound()
        {
            var result = await _controller.GetPollByCode("99999999");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CanVote_WithActivePoll_ReturnsOk()
        {
            var poll = new Poll { Question = "Test", Code = "12345678", Status = 0, Options = new List<Option>() };
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var result = await _controller.CanVote("12345678");
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CanVote_WithClosedPoll_ReturnsBadRequest()
        {
            var poll = new Poll { Question = "Test", Code = "12345678", Status = 1, Options = new List<Option>() };
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var result = await _controller.CanVote("12345678");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdatePoll_ClosePoll_ReturnsNoContent()
        {
            var poll = new Poll { Question = "Test", Code = "12345678", Status = 0, Options = new List<Option>() };
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var updated = new Poll { Question = "Test", Status = 1 };
            var result = await _controller.UpdatePoll("12345678", updated);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeletePoll_WithValidCode_ReturnsNoContent()
        {
            var poll = new Poll { Question = "Test", Code = "12345678", Status = 0, Options = new List<Option>() };
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var result = await _controller.DeletePoll("12345678");
            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Polls);
        }

        [Fact]
        public async Task DeletePoll_WithInvalidCode_ReturnsNotFound()
        {
            var result = await _controller.DeletePoll("99999999");
            Assert.IsType<NotFoundResult>(result);
        }
    }

    public class HttpClientFactoryMock : IHttpClientFactory
    {
        public HttpClient CreateClient(string name = "") => new HttpClient();
    }
}

