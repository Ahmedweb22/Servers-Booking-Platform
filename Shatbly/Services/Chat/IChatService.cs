namespace Shatbly.Services.Chat
{
        public interface IChatService
        {
            Task<ChatMessage> SendMessageAsync(
                string senderId,
                string receiverId,
                int bookingId,
                string message,
                CancellationToken cancellationToken = default);

            Task<IReadOnlyList<ChatMessage>> GetConversationAsync(
                string currentUserId,
                string otherUserId,
                int bookingId,
                CancellationToken cancellationToken = default);

            Task MarkAsReadAsync(
                int messageId,
                string currentUserId,
                CancellationToken cancellationToken = default);
        }
}
