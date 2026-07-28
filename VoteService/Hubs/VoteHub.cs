using Microsoft.AspNetCore.SignalR;

namespace VoteService.Hubs
{
    public class VoteHub : Hub
    {
        public async Task JoinPollRoom(string pollCode)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"poll_{pollCode}");
            await Clients.Caller.SendAsync("JoinedRoom", pollCode);
        }

        public async Task LeavePollRoom(string pollCode)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"poll_{pollCode}");
        }
    }
}
