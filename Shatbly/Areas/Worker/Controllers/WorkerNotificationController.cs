using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.Notification;
using System.Security.Claims;

namespace Shatbly.Areas.Worker.Controllers
{
    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_WORKER}")]
    public class WorkerNotificationController(INotificationService notificationService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> MyNotification(CancellationToken cancellationToken)
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
            var worktd = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(worktd))
            {
                return Unauthorized();
            }

            await notificationService.CreateNotificationAsync(
                worktd,
                "Test notification",
                "This is a test notification.",
                NotificationType.System,
                null,
                cancellationToken);

            return RedirectToAction(nameof(MyNotification));
        }
        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
