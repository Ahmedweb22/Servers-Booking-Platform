using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.Notification;
using System.Security.Claims;

namespace Shatbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]
    public class NotificationController(INotificationService notificationService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> MyNotification(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            ViewBag.CurrentUserId = userId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var notifications = await notificationService.GetUserNotificationsAsync(userId, cancellationToken);
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
        {
            await notificationService.MarkAsReadAsync(id, cancellationToken);
            return RedirectToAction(nameof(MyNotification));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await notificationService.DeleteAsync(id, cancellationToken);
            return RedirectToAction(nameof(MyNotification));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Test(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            await notificationService.CreateNotificationAsync(
                userId,
                "Test notification",
                "This is a test notification.",
                NotificationType.System,
                null,
                cancellationToken);

            return RedirectToAction(nameof(MyNotification));
        }
        [HttpGet]
        public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var notifications = await notificationService.GetUserNotificationsAsync(userId, cancellationToken);
            var result = notifications.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                isRead = n.IsRead,
                type = n.Type.ToString(),
                bookingId = n.BookingId,
                createdAt = n.CreatedAt.ToLocalTime().ToString("g")
            });
            return Json(result);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkAsReadApi(int id, CancellationToken cancellationToken)
        {
            await notificationService.MarkAsReadAsync(id, cancellationToken);
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessageToAdmin(string title, string message, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            {
                TempData["error-notification"] = "Title and message content are required. / العنوان ومحتوى الرسالة مطلوبان.";
                return RedirectToAction(nameof(MyNotification));
            }

            await notificationService.SendMessageToAdminAsync(userId, title, message, cancellationToken);

            TempData["success-notification"] = "Your message has been sent to support successfully. / تم إرسال رسالتك إلى الإدارة بنجاح.";
            return RedirectToAction(nameof(MyNotification));
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
