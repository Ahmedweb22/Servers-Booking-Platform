using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shtbly.DataAccess;

namespace Shtbly.Hubs
{
    [Authorize]
    public class TrackingHub(ApplicationDbContext context) : Hub
    {
        public async Task JoinBookingTracking(string bookingId)
        {
            if (!await CanAccessBookingAsync(bookingId))
            {
                throw new HubException("Not authorized to track this booking.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, bookingId);
        }

        public async Task SendLocation(string bookingId, double latitude, double longitude)
        {
            if (!await CanAccessBookingAsync(bookingId))
            {
                throw new HubException("Not authorized to send tracking updates for this booking.");
            }

            if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            {
                throw new HubException("Invalid coordinates.");
            }

            await Clients.Group(bookingId).SendAsync("ReceiveLocation", latitude, longitude);
        }

        private async Task<bool> CanAccessBookingAsync(string bookingId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(bookingId, out var parsedBookingId))
            {
                return false;
            }

            if (Context.User?.IsInRole(SD.ROLE_ADMIN) == true ||
                Context.User?.IsInRole(SD.ROLE_SUPER_ADMIN) == true)
            {
                return true;
            }

            var workerProfileId = await context.WorkerProfiles
                .Where(wp => wp.UserId == userId)
                .Select(wp => (int?)wp.Id)
                .FirstOrDefaultAsync();

            return await context.Bookings.AnyAsync(b =>
                b.Id == parsedBookingId &&
                (b.ClientId == userId || (workerProfileId.HasValue && b.WorkerId == workerProfileId.Value)));
        }
    }
}
