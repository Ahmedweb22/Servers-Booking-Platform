using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Shatbly.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public async Task JoinConversation(string conversationKey)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationKey);
        }

        public async Task LeaveConversation(string conversationKey)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationKey);
        }
    }
}
