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
        public async Task CreatePoll_WithValidData_ReturnsCreatedResult()
        {
            // Arrange
            var poll = new Poll
            {
                Question = "What is your favorite color?",
                QuestionType = 1, // Multiple Choice
                Options = new List<Option>
                {
                    new Option { Text = "Red" },
                    new Option { Text = "Blue" }
                }
            };

            // Act
            var result = await _controller.CreatePoll(poll);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
            
            var returnedPoll = createdResult.Value;
            Assert.NotNull(returnedPoll);
        }

        [Fact]
        public async Task CreatePoll_WithEmptyQuestion_ReturnsBadRequest()
        {
            // Arrange
            var poll = new Poll
            {
                Question = "",
                QuestionType = 1,
                Options = new List<Option>
                {
                    new Option { Text = "Red" },
                    new Option { Text = "Blue" }
                }
            };

            // Act
            var result = await _controller.CreatePoll(poll);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task CreatePoll_WithInvalidQuestionType_ReturnsBadRequest()
        {
            // Arrange
            var poll = new Poll
            {
                Question = "Test question",
                QuestionType = 5, // Invalid: must be 1-4
                Options = new List<Option>()
            };

            // Act
            var result = await _controller.CreatePoll(poll);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task CreatePoll_MultipleChoice_WithLessThanTwoOptions_ReturnsBadRequest()
        {
            // Arrange
            var poll = new Poll
            {
                Question = "Test",
                QuestionType = 1, // Multiple Choice
                Options = new List<Option>
                {
                    new Option { Text = "Only one option" }
                }
            };

            // Act
            var result = await _controller.CreatePoll(poll);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task CreatePoll_GeneratesUniqueCode()
        {
            // Arrange
            var poll1 = new Poll
            {
                Question = "Poll 1",
                QuestionType = 2, // Yes/No
                Options = new List<Option>()
            };

            var poll2 = new Poll
            {
                Question = "Poll 2",
                QuestionType = 2, // Yes/No
                Options = new List<Option>()
            };

            // Act
            await _controller.CreatePoll(poll1);
            await _controller.CreatePoll(poll2);

            var polls = _context.Polls.ToList();

            // Assert
            Assert.Equal(2, polls.Count);
            Assert.NotEqual(polls[0].Code, polls[1].Code);
        }

        [Fact]
        public async Task GetPollByCode_WithValidCode_ReturnsPoll()
        {
            // Arrange
            var poll = new Poll
            {
                Question = "What time is it?",
                QuestionType = 2, // Yes/No
                Code = "12345678",
                Status = 0,
                Options = new List<Option>()
            };
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetPollByCode("12345678");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedPoll = Assert.IsType<Poll>(okResult.Value);
            Assert.Equal("12345678", returnedPoll.Code);
            Assert.Equal("What time is it?", returnedPoll.Question);
        }

        [Fact]
        public async Task GetPollByCode_WithInvalidCode_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetPollByCode("99999999");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CanVote_WithValidActivePoll_ReturnsTrue()
        {
            // Arrange
            var poll = new Poll
            {
                Question = "Test",
                Code = "12345678",
                Status = 0, // Active
                Options = new List<Option>()
            };
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.CanVote("12345678");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CanVote_WithClosedPoll_ReturnsBadRequest()
        {
            // Arrange
            var poll = new Poll
            {
                Question = "Test",
                Code = "12345678",
                Status = 1, // Closed
                Options = new List<Option>()
            };
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.CanVote("12345678");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task UpdatePoll_ChangeStatus_ReturnsNoContent()
        {
            // Arrange
            var poll = new Poll
            {
                Question = "Test",
                Code = "12345678",
                Status = 0, // Active
                Options = new List<Option>()
            };
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            var updatedPoll = new Poll
            {
                Question = "Test",
                Status = 1 // Close the poll
            };

            // Act
            var result = await _controller.UpdatePoll("12345678", updatedPoll);

            // Assert
            Assert.IsType<NoContentResult>(result);

            var pollInDb = _context.Polls.First(p => p.Code == "12345678");
            Assert.Equal(1, pollInDb.Status);
        }

        [Fact]
        public async Task DeletePoll_WithValidCode_ReturnsNoContent()
        {
            // Arrange
            var poll = new Poll
            {
                Question = "Test",
                Code = "12345678",
                Status = 0,
                Options = new List<Option>()
            };
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeletePoll("12345678");

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Polls);
        }

        [Fact]
        public async Task DeletePoll_WithInvalidCode_ReturnsNotFound()
        {
            // Act
            var result = await _controller.DeletePoll("99999999");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }

    // Mock HttpClientFactory for testing
    public class HttpClientFactoryMock : IHttpClientFactory
    {
        public HttpClient CreateClient(string name = "")
        {
            return new HttpClient();
        }
    }
}
