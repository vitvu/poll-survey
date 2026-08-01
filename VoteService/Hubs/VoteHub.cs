using Microsoft.AspNetCore.SignalR;

namespace VoteService.Hubs
{
    public class VoteHub : Hub
    {
        /// <summary>
        /// Client joins the poll room by PollCode to receive real-time updates
        /// </summary>
        public async Task JoinPollRoom(string pollCode)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"poll_{pollCode}");
            await Clients.Caller.SendAsync("JoinedRoom", pollCode);
        }

        /// <summary>
        /// Client leaves the poll room
        /// </summary>
        public async Task LeavePollRoom(string pollCode)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"poll_{pollCode}");
        }

        /// <summary>
        /// Broadcast new vote to all clients viewing results
        /// </summary>
        public async Task BroadcastVoteUpdate(string pollCode, object voteData)
        {
            await Clients.Group($"poll_{pollCode}").SendAsync("VoteUpdated", voteData);
        }
    }
}
