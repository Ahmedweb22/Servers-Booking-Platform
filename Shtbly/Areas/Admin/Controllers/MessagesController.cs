using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shtbly.DataAccess;
using Shtbly.Models;
using Shtbly.Services.Notification;
using Shtbly.Utilities;
using Shtbly.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Shtbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN}")]
    public class MessagesController : Controller
    {
        private readonly Shtbly.UnitOfWork.IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;

        public MessagesController(
            Shtbly.UnitOfWork.IUnitOfWork unitOfWork,
            UserManager<User> userManager,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: Admin/Messages
        public async Task<IActionResult> Index()
        {
            // Self-healing pass: auto-confirm emails for all already approved workers
            var approvedUnconfirmedWorkers = await _unitOfWork.WorkerProfiles.GetAsync(
                expression: wp => wp.IsApproved && wp.User != null && !wp.User.EmailConfirmed,
                includes: new System.Linq.Expressions.Expression<System.Func<WorkerProfile, object>>[] { wp => wp.User! }
            );

            if (approvedUnconfirmedWorkers.Any())
            {
                foreach (var wp in approvedUnconfirmedWorkers)
                {
                    wp.User.EmailConfirmed = true;
                }
                await _unitOfWork.CommitAsync();
            }

            var allWorkerProfiles = await _unitOfWork.WorkerProfiles.GetAsync(
                expression: wp => !wp.IsApproved,
                includes: new System.Linq.Expressions.Expression<System.Func<WorkerProfile, object>>[] { wp => wp.User! }
            );
            var pendingWorkers = allWorkerProfiles.OrderByDescending(wp => wp.CreatedAt).ToList();

            var allUsers = await _unitOfWork.Users.GetAsync(tracking: false);
            var users = allUsers.OrderBy(u => u.FName).ThenBy(u => u.LName).ToList();

            var currentUserId = _userManager.GetUserId(User);

            var adminUserIds = (await _userManager.GetUsersInRoleAsync(SD.ROLE_ADMIN))
                .Select(u => u.Id)
                .Concat((await _userManager.GetUsersInRoleAsync(SD.ROLE_SUPER_ADMIN)).Select(u => u.Id))
                .Distinct()
                .ToList();

            var notificationsList = await _unitOfWork.Notifications.GetAsync(
                includes: new System.Linq.Expressions.Expression<System.Func<Notification, object>>[] { n => n.User! },
                tracking: false
            );
            var allNotifications = notificationsList
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToList();

            var incomingMessages = allNotifications
                .Where(n => currentUserId != null ? n.UserId == currentUserId : adminUserIds.Contains(n.UserId))
                .ToList();

            var sentNotifications = allNotifications
                .Where(n => !adminUserIds.Contains(n.UserId))
                .ToList();

            foreach (var notif in incomingMessages)
            {
                if (notif.Message != null && notif.Message.StartsWith("[SenderId: "))
                {
                    var parts = notif.Message.Split(']', 2);
                    var senderId = parts[0].Replace("[SenderId: ", "").Trim();
                    var sender = await _userManager.FindByIdAsync(senderId);
                    if (sender != null)
                    {
                        notif.User = sender;
                    }
                    notif.Message = parts[1].Trim();
                }
            }

            var vm = new AdminMessagesVM
            {
                PendingWorkers = pendingWorkers,
                Users = users,
                SentNotifications = sentNotifications,
                IncomingMessages = incomingMessages
            };

            return View(vm);
        }

        // POST: Admin/Messages/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(AdminMessagesVM model)
        {
            if (string.IsNullOrWhiteSpace(model.TargetUserId))
            {
                TempData["error-notification"] = "Please select a recipient. / الرجاء اختيار مستلم.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.MessageTitle) || string.IsNullOrWhiteSpace(model.MessageBody))
            {
                TempData["error-notification"] = "Title and message content are required. / العنوان ومحتوى الرسالة مطلوبان.";
                return RedirectToAction(nameof(Index));
            }

            await _notificationService.CreateNotificationAsync(
                model.TargetUserId,
                model.MessageTitle,
                model.MessageBody,
                model.MessageType
            );

            TempData["success-notification"] = "Message/Notification sent successfully. / تم إرسال الرسالة/التنبيه بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Messages/ApproveWorker
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveWorker(int profileId)
        {
            var profile = await _unitOfWork.WorkerProfiles.GetOneAsync(
                expression: wp => wp.Id == profileId,
                includes: new System.Linq.Expressions.Expression<System.Func<WorkerProfile, object>>[] { wp => wp.User! },
                tracking: true);

            if (profile == null)
            {
                TempData["error-notification"] = "Worker profile not found. / لم يتم العثور على ملف الفني.";
                return NotFound();
            }

            profile.IsApproved = true;
            profile.IsVerified = true;

            var user = profile.User;
            if (user != null)
            {
                user.EmailConfirmed = true; // Auto-confirm email on approval

                // Remove client role if they have it, and add to worker role
                if (await _userManager.IsInRoleAsync(user, SD.ROLE_CUSTOMER))
                {
                    await _userManager.RemoveFromRoleAsync(user, SD.ROLE_CUSTOMER);
                }

                if (!await _userManager.IsInRoleAsync(user, SD.ROLE_WORKER))
                {
                    await _userManager.AddToRoleAsync(user, SD.ROLE_WORKER);
                }
            }

            await _unitOfWork.CommitAsync();

            // Send notification to the newly approved worker
            await _notificationService.CreateNotificationAsync(
                profile.UserId,
                "Application Approved / تم قبول طلبك",
                "Your application to register as a worker has been approved! You can now log in and access services. / تم قبول طلب انضمامك كفني بنجاح! يمكنك الآن تسجيل الدخول وإدارة خدماتك.",
                NotificationType.System
            );

            TempData["success-notification"] = "Worker application approved successfully. / تم قبول طلب الفني بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Messages/RejectWorker
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectWorker(int profileId, string? rejectionReason)
        {
            var profile = await _unitOfWork.WorkerProfiles.GetOneAsync(
                expression: wp => wp.Id == profileId,
                includes: new System.Linq.Expressions.Expression<System.Func<WorkerProfile, object>>[] { wp => wp.User! },
                tracking: true);

            if (profile == null)
            {
                TempData["error-notification"] = "Worker profile not found. / لم يتم العثور على ملف الفني.";
                return NotFound();
            }

            var userId = profile.UserId;

            // Send notification explaining rejection
            var reasonMessage = string.IsNullOrWhiteSpace(rejectionReason)
                ? "Your application to register as a worker was not approved. / لم يتم قبول طلب انضمامك كفني."
                : $"Your application to register as a worker was not approved for the following reason: {rejectionReason} / لم يتم قبول طلب انضمامك كفني للسبب التالي: {rejectionReason}";

            await _notificationService.CreateNotificationAsync(
                userId,
                "Application Status Update / تحديث حالة الطلب",
                reasonMessage,
                NotificationType.System
            );

            // Delete CV if exists
            if (!string.IsNullOrEmpty(profile.CVPath))
            {
                var cvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\img\\worker\\worker_cv", profile.CVPath);
                if (System.IO.File.Exists(cvFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(cvFilePath);
                    }
                    catch (Exception)
                    {
                        // Log or ignore if file couldn't be deleted
                    }
                }
            }

            _unitOfWork.WorkerProfiles.Delete(profile);
            await _unitOfWork.CommitAsync();

            TempData["success-notification"] = "Worker application rejected and removed. / تم رفض الطلب وحذفه.";
            return RedirectToAction(nameof(Index));
        }
    }
}
