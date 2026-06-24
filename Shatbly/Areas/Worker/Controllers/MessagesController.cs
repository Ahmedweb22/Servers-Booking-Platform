using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.Notification;
using System.Security.Claims;

namespace Shatbly.Areas.Worker.Controllers
{
    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_WORKER}")]
    public class MessagesController(INotificationService notificationService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
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
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await notificationService.DeleteAsync(id, cancellationToken);
            return RedirectToAction(nameof(Index));
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
                return RedirectToAction(nameof(Index));
            }

            await notificationService.SendMessageToAdminAsync(userId, title, message, cancellationToken);

            TempData["success-notification"] = "Your message has been sent to support successfully. / تم إرسال رسالتك إلى الإدارة بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
