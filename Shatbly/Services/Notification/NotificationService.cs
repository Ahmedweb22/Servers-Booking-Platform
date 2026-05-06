using Microsoft.AspNetCore.SignalR;
using Shatbly.Hubs;
using Shatbly.Models;
using Shatbly.UnitOfWork;
using NotificationModel = Shatbly.Models.Notification;

namespace Shatbly.Services.Notification
{
    public class NotificationService(
        IUnitOfWork unitOfWork,
        IHubContext<NotificationHub> hubContext) : INotificationService
    {
        public async Task<NotificationModel> CreateNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type,
            int? bookingId = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            var notification = new NotificationModel
            {
                UserId = userId.Trim(),
                Title = title.Trim(),
                Message = message.Trim(),
                Type = type,
                BookingId = bookingId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await unitOfWork.Notifications.CreateAsync(notification);
            await unitOfWork.CommitAsync();

            await hubContext.Clients
                .User(notification.UserId)
                .SendAsync("ReceiveNotification", notification, cancellationToken);

            return notification;
        }

        public async Task<IReadOnlyList<NotificationModel>> GetUserNotificationsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var notifications = await unitOfWork.Notifications.GetAsync(
                n => n.UserId == userId,
                tracking: false);

            return notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public async Task MarkAsReadAsync(
            int notificationId,
            CancellationToken cancellationToken = default)
        {
            var notification = await unitOfWork.Notifications.GetOneAsync(
                n => n.Id == notificationId);

            if (notification is null)
            {
                return;
            }

            notification.IsRead = true;
            unitOfWork.Notifications.Update(notification);
            await unitOfWork.CommitAsync();
        }

        public async Task DeleteAsync(
            int notificationId,
            CancellationToken cancellationToken = default)
        {
            var notification = await unitOfWork.Notifications.GetOneAsync(
                n => n.Id == notificationId);

            if (notification is null)
            {
                return;
            }

            unitOfWork.Notifications.Delete(notification);
            await unitOfWork.CommitAsync();
        }
    }
}
