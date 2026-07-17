using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using Shtbly.Hubs;
using Shtbly.Models;
using Shtbly.UnitOfWork;
using Shtbly.Utilities;
using NotificationModel = Shtbly.Models.Notification;

namespace Shtbly.Services.Notification
{
    public class NotificationService(
        IUnitOfWork unitOfWork,
        IHubContext<NotificationHub> hubContext,
        UserManager<User> userManager) : INotificationService
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

        public async Task SendMessageToAdminAsync(
            string senderId,
            string title,
            string message,
            CancellationToken cancellationToken = default)
        {
            var admins = await userManager.GetUsersInRoleAsync(SD.ROLE_ADMIN);
            var superAdmins = await userManager.GetUsersInRoleAsync(SD.ROLE_SUPER_ADMIN);
            var adminUsers = admins.Concat(superAdmins).GroupBy(u => u.Id).Select(g => g.First()).ToList();

            var formattedMessage = $"[SenderId: {senderId}] {message}";

            if (!adminUsers.Any())
            {
                var fallbackAdmin = await userManager.FindByEmailAsync("Admin@gmail.com") 
                                 ?? await userManager.FindByEmailAsync("SuperAdmin@gmail.com");
                if (fallbackAdmin != null)
                {
                    adminUsers.Add(fallbackAdmin);
                }
            }

            foreach (var admin in adminUsers)
            {
                await CreateNotificationAsync(
                    admin.Id,
                    title,
                    formattedMessage,
                    NotificationType.Message,
                    null,
                    cancellationToken
                );
            }
        }
    }
}
