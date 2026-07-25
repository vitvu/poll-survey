using Microsoft.AspNetCore.SignalR;

namespace VoteService.Hubs
{
    public class VoteHub : Hub
    {
        /// <summary>
        /// Client tham gia vào phòng theo PollCode để nhận real-time updates
        /// </summary>
        public async Task JoinPollRoom(string pollCode)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"poll_{pollCode}");
            await Clients.Caller.SendAsync("JoinedRoom", pollCode);
        }

        /// <summary>
        /// Client rời khỏi phòng
        /// </summary>
        public async Task LeavePollRoom(string pollCode)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"poll_{pollCode}");
        }

        /// <summary>
        /// Broadcast vote mới đến tất cả clients đang xem kết quả
        /// </summary>
        public async Task BroadcastVoteUpdate(string pollCode, object voteData)
        {
            await Clients.Group($"poll_{pollCode}").SendAsync("VoteUpdated", voteData);
        }
    }
}
