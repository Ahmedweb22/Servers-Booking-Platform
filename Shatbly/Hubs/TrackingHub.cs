using Microsoft.AspNetCore.SignalR;

namespace Shatbly.Hubs
{
    public class TrackingHub : Hub
    {
        public async Task JoinBookingTracking(string bookingId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, bookingId);
        }
        public async Task SendLocation(string bookingId, double latitude, double longitude)
        {
            await Clients.Group(bookingId).SendAsync("ReceiveLocation", latitude, longitude);
        }
    }
}
