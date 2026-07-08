using Microsoft.AspNetCore.SignalR;
using Shatbly.Hubs;
using Shatbly.Models;
using Shatbly.Services.Notification;
using Shatbly.UnitOfWork;

namespace Shatbly.Services.Chat
{
    public class ChatService(
        IUnitOfWork unitOfWork,
        IHubContext<ChatHub> chatHub,
        INotificationService notificationService) : IChatService
    {
        public async Task<ChatMessage> SendMessageAsync(
            string senderId,
            string receiverId,
            int bookingId,
            string? message,
            string? imageUrl = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(senderId);
            ArgumentException.ThrowIfNullOrWhiteSpace(receiverId);

            if (string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(imageUrl))
            {
                throw new ArgumentException("Cannot send an empty message without an image.");
            }

            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                BookingId = bookingId,
                Message = string.IsNullOrWhiteSpace(message) ? null : System.Net.WebUtility.HtmlEncode(message.Trim()),
                ImageUrl = imageUrl,
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            await unitOfWork.ChatMessages.CreateAsync(chatMessage);
            await unitOfWork.CommitAsync();

            var conversationKey = GetConversationKey(senderId, receiverId, bookingId);

            await chatHub.Clients.Group(conversationKey).SendAsync(
                "ReceiveMessage",
                new
                {
                    chatMessage.Id,
                    chatMessage.SenderId,
                    chatMessage.ReceiverId,
                    chatMessage.BookingId,
                    chatMessage.Message,
                    chatMessage.ImageUrl,
                    chatMessage.IsRead,
                    chatMessage.SentAt
                },
                cancellationToken);

            //await notificationService.CreateNotificationAsync(
            //    receiverId,
            //    "New message",
            //    "You have received a new message.",
            //    NotificationType.Message,
            //    bookingId,
            //    cancellationToken);
            var notificationContent = string.IsNullOrWhiteSpace(message)
                ? "أرسل صورة / Sent an image"
                : message;

            await notificationService.CreateNotificationAsync(
                receiverId,
                "New message",
                notificationContent,
                NotificationType.Message,
                bookingId,
                cancellationToken);
            //        await notificationService.CreateNotificationAsync(
            //receiverId,
            //"New message",
            //"Click to open conversation",
            //NotificationType.Message,
            //bookingId,
            //cancellationToken);
            return chatMessage;
        }

        public async Task<IReadOnlyList<ChatMessage>> GetConversationAsync(
            string currentUserId,
            string otherUserId,
            int bookingId,
            CancellationToken cancellationToken = default)
        {
            var messages = await unitOfWork.ChatMessages.GetAsync(m =>
                m.BookingId == bookingId &&
                ((m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                 (m.SenderId == otherUserId && m.ReceiverId == currentUserId)),
                tracking: false);

            return messages
                .OrderBy(m => m.SentAt)
                .ToList();
        }

        public async Task MarkAsReadAsync(
            int messageId,
            string currentUserId,
            CancellationToken cancellationToken = default)
        {
            var message = await unitOfWork.ChatMessages.GetOneAsync(m =>
                m.Id == messageId &&
                m.ReceiverId == currentUserId);

            if (message is null)
            {
                return;
            }

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;

            unitOfWork.ChatMessages.Update(message);
            await unitOfWork.CommitAsync();

            var conversationKey = GetConversationKey(message.SenderId, message.ReceiverId, message.BookingId);

            await chatHub.Clients.Group(conversationKey).SendAsync(
                "MessageRead",
                new
                {
                    message.Id,
                    message.ReadAt
                },
                cancellationToken);
        }

        private static string GetConversationKey(string userA, string userB, int bookingId)
        {
            var users = new[] { userA, userB }.OrderBy(x => x).ToArray();
            return $"booking-{bookingId}-chat-{users[0]}-{users[1]}";
        }
    }
}
