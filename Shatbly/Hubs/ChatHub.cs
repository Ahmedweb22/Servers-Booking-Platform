using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shatbly.Services.Chat;

namespace Shatbly.Hubs
{
    [Authorize]
    public class ChatHub(IChatService chatService) : Hub
    {
        public async Task JoinConversation(string conversationKey)
        {
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
    }
}
