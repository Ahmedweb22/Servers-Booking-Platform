using Shtbly.Models;
using NotificationModel = Shtbly.Models.Notification;

namespace Shtbly.Services.Notification
{
    public interface INotificationService
    {
        Task<NotificationModel> CreateNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type,
            int? bookingId = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<NotificationModel>> GetUserNotificationsAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task MarkAsReadAsync(
            int notificationId,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            int notificationId,
            CancellationToken cancellationToken = default);

        Task SendMessageToAdminAsync(
            string senderId,
            string title,
            string message,
            CancellationToken cancellationToken = default);
    }
}
