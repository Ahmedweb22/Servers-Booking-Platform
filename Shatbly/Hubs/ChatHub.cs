using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shtbly.DataAccess;
using Shtbly.Services.Chat;

namespace Shtbly.Hubs
{
    [Authorize]
    public class ChatHub(IChatService chatService, ApplicationDbContext context) : Hub
    {
        public async Task JoinConversation(string conversationKey)
        {
            if (!await CanJoinConversationAsync(conversationKey))
            {
                throw new HubException("Not authorized to join this conversation.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, conversationKey);
        }

        public async Task LeaveConversation(string conversationKey)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationKey);
        }

        public async Task MarkAsRead(int messageId)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await chatService.MarkAsReadAsync(messageId, userId);
            }
        }

        private async Task<bool> CanJoinConversationAsync(string conversationKey)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            if (Context.User?.IsInRole(SD.ROLE_ADMIN) == true ||
                Context.User?.IsInRole(SD.ROLE_SUPER_ADMIN) == true)
            {
                return true;
            }

            if (!TryGetBookingId(conversationKey, out var bookingId))
            {
                return false;
            }

            var workerProfileId = await context.WorkerProfiles
                .Where(wp => wp.UserId == userId)
                .Select(wp => (int?)wp.Id)
                .FirstOrDefaultAsync();

            return await context.Bookings.AnyAsync(b =>
                b.Id == bookingId &&
                (b.ClientId == userId || (workerProfileId.HasValue && b.WorkerId == workerProfileId.Value)));
        }

        private static bool TryGetBookingId(string conversationKey, out int bookingId)
        {
            bookingId = 0;
            const string prefix = "booking-";
            const string separator = "-chat-";

            if (string.IsNullOrWhiteSpace(conversationKey) ||
                !conversationKey.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var separatorIndex = conversationKey.IndexOf(separator, prefix.Length, StringComparison.Ordinal);
            if (separatorIndex <= prefix.Length)
            {
                return false;
            }

            var bookingIdPart = conversationKey.Substring(prefix.Length, separatorIndex - prefix.Length);
            return int.TryParse(bookingIdPart, out bookingId);
        }
    }
}
