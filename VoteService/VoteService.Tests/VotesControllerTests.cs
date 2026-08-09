using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Xunit;
using VoteService.Controllers;
using VoteService.Data;
using VoteService.Hubs;
using VoteService.Models;

namespace VoteService.Tests
{
    public class VotesControllerTests : IDisposable
    {
        private readonly VoteDbContext _context;
        private readonly VotesController _controller;
        private readonly HttpClientFactoryMock _httpClientFactory;
        private readonly HubContextMock _hubContextMock;
        private readonly ConfigurationMock _configurationMock;

        public VotesControllerTests()
        {
            var options = new DbContextOptionsBuilder<VoteDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new VoteDbContext(options);
            _httpClientFactory = new HttpClientFactoryMock();
            _hubContextMock = new HubContextMock();
            _configurationMock = new ConfigurationMock();

            _controller = new VotesController(
                _context,
                _httpClientFactory,
                _configurationMock,
                _hubContextMock.Object
            );
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task SubmitVote_WithValidData_ReturnsOk()
        {
            // Arrange
            _httpClientFactory.SetupSuccess();
            var vote = new Vote
            {
                PollCode = "12345678",
                OptionId = 1,
                VoteValue = "",
                VoterToken = "token-123"
            };

            // Act
            var result = await _controller.SubmitVote(vote);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task SubmitVote_WithMissingPollCode_ReturnsBadRequest()
        {
            // Arrange
            var vote = new Vote
            {
                PollCode = "",
                OptionId = 1,
                VoteValue = "",
                VoterToken = "token-123"
            };

            // Act
            var result = await _controller.SubmitVote(vote);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SubmitVote_WithMissingVoterToken_ReturnsBadRequest()
        {
            // Arrange
            var vote = new Vote
            {
                PollCode = "12345678",
                OptionId = 1,
                VoteValue = "",
                VoterToken = ""
            };

            // Act
            var result = await _controller.SubmitVote(vote);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SubmitVote_WithNoOptionIdAndNoVoteValue_ReturnsBadRequest()
        {
            // Arrange
            var vote = new Vote
            {
                PollCode = "12345678",
                OptionId = 0,
                VoteValue = "",
                VoterToken = "token-123"
            };

            // Act
            var result = await _controller.SubmitVote(vote);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SubmitVote_WhenAlreadyVoted_ReturnsBadRequest()
        {
            // Arrange
            _httpClientFactory.SetupSuccess();
            var existingVote = new Vote
            {
                PollCode = "12345678",
                OptionId = 1,
                VoteValue = "",
                VoterToken = "token-123"
            };
            _context.Votes.Add(existingVote);
            await _context.SaveChangesAsync();

            var duplicateVote = new Vote
            {
                PollCode = "12345678",
                OptionId = 2,
                VoteValue = "",
                VoterToken = "token-123" // Same voter token
            };

            // Act
            var result = await _controller.SubmitVote(duplicateVote);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SubmitVote_WhenPollClosed_ReturnsBadRequest()
        {
            // Arrange
            _httpClientFactory.SetupFailure(); // Poll service returns error
            var vote = new Vote
            {
                PollCode = "99999999",
                OptionId = 1,
                VoteValue = "",
                VoterToken = "token-123"
            };

            // Act
            var result = await _controller.SubmitVote(vote);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GetVoteData_WithValidPollCode_ReturnsVoteData()
        {
            // Arrange
            var votes = new List<Vote>
            {
                new Vote { PollCode = "12345678", OptionId = 1, VoteValue = "", VoterToken = "token-1" },
                new Vote { PollCode = "12345678", OptionId = 1, VoteValue = "", VoterToken = "token-2" },
                new Vote { PollCode = "12345678", OptionId = 2, VoteValue = "", VoterToken = "token-3" }
            };
            _context.Votes.AddRange(votes);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetVoteData("12345678");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            // Should have 3 total votes
        }

        [Fact]
        public async Task GetVoteData_WithInvalidPollCode_ReturnsEmptyData()
        {
            // Act
            var result = await _controller.GetVoteData("99999999");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task DeleteVotes_WithValidPollCode_ReturnsNoContent()
        {
            // Arrange
            var votes = new List<Vote>
            {
                new Vote { PollCode = "12345678", OptionId = 1, VoteValue = "", VoterToken = "token-1" },
                new Vote { PollCode = "12345678", OptionId = 2, VoteValue = "", VoterToken = "token-2" }
            };
            _context.Votes.AddRange(votes);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteVotes("12345678");

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Votes);
        }

        [Fact]
        public async Task DeleteVotes_WithMissingPollCode_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteVotes("");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task BroadcastPollClosed_WithValidPollCode_ReturnsOk()
        {
            // Arrange
            var request = new PollClosedRequest { PollCode = "12345678" };

            // Act
            var result = await _controller.BroadcastPollClosed(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task BroadcastPollClosed_WithMissingPollCode_ReturnsBadRequest()
        {
            // Arrange
            var request = new PollClosedRequest { PollCode = "" };

            // Act
            var result = await _controller.BroadcastPollClosed(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SubmitVote_WithOpenTextQuestion_StoresVoteValue()
        {
            // Arrange
            _httpClientFactory.SetupSuccess();
            var vote = new Vote
            {
                PollCode = "87654321",
                OptionId = 0,
                VoteValue = "This is an open text answer",
                VoterToken = "token-456"
            };

            // Act
            var result = await _controller.SubmitVote(vote);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var storedVote = _context.Votes.First();
            Assert.Equal("This is an open text answer", storedVote.VoteValue);
        }
    }

    // Mock IHubContext for testing
    public class HubContextMock
    {
        public IHubContext<VoteHub> Object { get; }

        public HubContextMock()
        {
            Object = new MockHubContext();
        }
    }

    public class MockHubContext : IHubContext<VoteHub>
    {
        public IHubClients<VoteHub> Clients => new MockHubClients();
        public IGroupManager Groups => new MockGroupManager();
    }

    public class MockHubClients : IHubClients<VoteHub>
    {
        public IClientProxy All => new MockClientProxy();
        public IClientProxy AllExcept(params string[] excludedConnectionIds) => new MockClientProxy();
        public IClientProxy Client(string connectionId) => new MockClientProxy();
        public IClientProxy Clients(params string[] connectionIds) => new MockClientProxy();
        public IClientProxy Group(string groupName) => new MockClientProxy();
        public IClientProxy GroupExcept(string groupName, params string[] excludedConnectionIds) => new MockClientProxy();
        public IClientProxy Groups(params string[] groupNames) => new MockClientProxy();
        public IClientProxy OthersInGroup(string groupName) => new MockClientProxy();
        public IClientProxy User(string userId) => new MockClientProxy();
        public IClientProxy Users(params string[] userIds) => new MockClientProxy();
    }

    public class MockClientProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class MockGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveFromAllGroupsAsync(string connectionId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    // Mock HttpClientFactory
    public class HttpClientFactoryMock : IHttpClientFactory
    {
        private bool _shouldSucceed = false;

        public void SetupSuccess()
        {
            _shouldSucceed = true;
        }

        public void SetupFailure()
        {
            _shouldSucceed = false;
        }

        public HttpClient CreateClient(string name = "")
        {
            var handler = new MockHttpMessageHandler(_shouldSucceed);
            return new HttpClient(handler);
        }
    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly bool _shouldSucceed;

        public MockHttpMessageHandler(bool shouldSucceed = true)
        {
            _shouldSucceed = shouldSucceed;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = _shouldSucceed
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(HttpStatusCode.BadRequest);

            return Task.FromResult(response);
        }
    }

    // Mock IConfiguration
    public class ConfigurationMock : Microsoft.Extensions.Configuration.IConfiguration
    {
        public string? this[string key]
        {
            get => key == "Services:PollServiceUrl" ? "http://pollservice" : null;
            set { }
        }

        public IEnumerable<IConfigurationSection> GetChildren() => new List<IConfigurationSection>();
        public IChangeToken GetReloadToken() => new MockChangeToken();
        public IConfigurationSection GetSection(string key) => new MockConfigurationSection();
    }

    public class MockConfigurationSection : Microsoft.Extensions.Configuration.IConfigurationSection
    {
        public string this[string key]
        {
            get => "";
            set { }
        }

        public string? Value { get; set; }
        public string Key { get; }
        public string Path { get; }
        public IEnumerable<IConfigurationSection> GetChildren() => new List<IConfigurationSection>();
        public IChangeToken GetReloadToken() => new MockChangeToken();
        public IConfigurationSection GetSection(string key) => new MockConfigurationSection();
    }

    public class MockChangeToken : Microsoft.Extensions.Primitives.IChangeToken
    {
        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new MockDisposable();
    }

    public class MockDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
