using Microsoft.AspNetCore.SignalR;

namespace VoteService.Hubs
{
    public class VoteHub : Hub
    {
        // joins a poll room to receive real-time vote updates
        public async Task JoinPollRoom(string pollCode)
        {
            // add this client connection to poll group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"poll_{pollCode}");
            // send confirmation to client
            await Clients.Caller.SendAsync("JoinedRoom", pollCode);
        }

        // leaves a poll room
        public async Task LeavePollRoom(string pollCode)
        {
            // remove this client connection from poll group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"poll_{pollCode}");
        }

        // broadcasts vote updates to all clients in poll room
        public async Task BroadcastVoteUpdate(string pollCode, object voteData)
        {
            // send vote updated event to all clients in poll group
            await Clients.Group($"poll_{pollCode}").SendAsync("VoteUpdated", voteData);
        }
    }
}
