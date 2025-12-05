using Microsoft.AspNetCore.SignalR;

namespace StreamAudio.Hubs
{
    public class WebRTCHub : Hub
    {
        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("user-joined", Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Clients.All.SendAsync("user-left", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        // gửi offer
        public async Task SendOffer(string roomId, string receiverId, object offer)
        {
            await Clients.Client(receiverId).SendAsync("receive-offer", Context.ConnectionId, offer);
        }

        // gửi answer
        public async Task SendAnswer(string roomId, string receiverId, object answer)
        {
            await Clients.Client(receiverId).SendAsync("receive-answer", Context.ConnectionId, answer);
        }

        // gửi ICE
        public async Task SendIceCandidate(string roomId, string receiverId, object candidate)
        {
            await Clients.Client(receiverId).SendAsync("receive-ice", Context.ConnectionId, candidate);
        }
    }
}
