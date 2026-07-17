using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Shtbly.Services.Notification;

namespace Shtbly.ViewComponents
{
    public class UnreadNotificationsViewComponent(
        INotificationService notificationService) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return View(0);
            }

            var notifications = await notificationService.GetUserNotificationsAsync(userId);
            var unreadCount = notifications.Count(n => !n.IsRead);

            return View(unreadCount);
        }
    }
}
