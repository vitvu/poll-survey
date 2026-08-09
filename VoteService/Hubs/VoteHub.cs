using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace VoteService.Hubs
{
    public class VoteHub : Hub
    {
        // Called when client joins a poll room to receive live updates
        public async Task JoinPollRoom(string pollCode)
        {
            string groupName = $"poll_{pollCode}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Caller.SendAsync("UserJoined", new { pollCode = pollCode });
        }

        // Called when client leaves a poll room
        public async Task LeavePollRoom(string pollCode)
        {
            string groupName = $"poll_{pollCode}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
