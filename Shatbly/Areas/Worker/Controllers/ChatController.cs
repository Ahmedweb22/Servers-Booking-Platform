using global::Shtbly.Services.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Shtbly.Services.File_Service;
using Shtbly.Services.Chat;

namespace Shtbly.Areas.Worker.Controllers
{
    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_WORKER},{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN}")]
    public class ChatController : Controller
    {
        private readonly Shtbly.UnitOfWork.IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IChatService _chatService;
        private readonly IFileService _fileService;

        public ChatController(
            Shtbly.UnitOfWork.IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IChatService chatService,
            IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _chatService = chatService;
            _fileService = fileService;
        }

            private string? CurrentUserId =>
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            [HttpGet]
            public async Task<IActionResult> Conversation(
                int bookingId,
                CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(CurrentUserId))
                    return Unauthorized();

                var workerProfile = await _unitOfWork.WorkerProfiles.GetOneAsync(
                    expression: x => x.UserId == CurrentUserId,
                    tracking: false);

                if (workerProfile == null)
                    return Unauthorized();

                var booking = await _unitOfWork.Bookings.GetOneAsync(
                    expression: x => x.Id == bookingId,
                    includes: new System.Linq.Expressions.Expression<System.Func<Booking, object>>[]
                    {
                        x => x.Client!,
                        x => x.Worker!,
                        x => x.Worker!.User!
                    },
                    tracking: false);

                if (booking == null)
                    return NotFound();

                if (booking.WorkerId != workerProfile.Id)
                    return Forbid();

                var allMessages = await _unitOfWork.ChatMessages.GetAsync(
                    expression: x => x.BookingId == bookingId,
                    includes: new System.Linq.Expressions.Expression<System.Func<ChatMessage, object>>[]
                    {
                        x => x.Sender!,
                        x => x.Receiver!
                    },
                    tracking: true);
                var messages = allMessages.OrderBy(x => x.SentAt).ToList();

                var unreadMessages = messages
                    .Where(x =>
                        x.ReceiverId == CurrentUserId &&
                        !x.IsRead)
                    .ToList();

                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                    msg.ReadAt = DateTime.UtcNow;
                }

                await _unitOfWork.CommitAsync();

                var users = new[] { CurrentUserId, booking.ClientId }.OrderBy(x => x).ToArray();
                ViewBag.ConversationKey = $"booking-{booking.Id}-chat-{users[0]}-{users[1]}";

                var vm = new ChatWorkerViewModel
                {
                    BookingId = booking.Id,
                    ClientId = booking.ClientId,
                    ClientName = booking.Client.UserName,
                    Messages = messages,
                    ClientProfilePictureUrl = booking.Client.ProfilePicture,
                    WorkerProfilePictureUrl = !string.IsNullOrEmpty(booking.Worker.ProfilePicturePath) ? booking.Worker.ProfilePicturePath : booking.Worker.User.ProfilePicture
                };

                return View(vm);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Send(
                int bookingId,
                string? message,
                IFormFile? image,
                CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(CurrentUserId))
                    return Unauthorized();

                if (string.IsNullOrWhiteSpace(message) && (image == null || image.Length == 0))
                    return RedirectToAction(
                        nameof(Conversation),
                        new { bookingId });

                var workerProfile = await _unitOfWork.WorkerProfiles.GetOneAsync(
                    expression: x => x.UserId == CurrentUserId,
                    tracking: false);

                if (workerProfile == null)
                    return Unauthorized();

                var booking = await _unitOfWork.Bookings.GetOneAsync(
                    expression: x => x.Id == bookingId,
                    includes: new System.Linq.Expressions.Expression<System.Func<Booking, object>>[] { x => x.Client! },
                    tracking: false);

                if (booking == null)
                    return NotFound();

                if (booking.WorkerId != workerProfile.Id)
                    return Forbid();

                string? imageUrl = null;
                if (image != null && image.Length > 0)
                {
                    var uploadResult = await _fileService.UploadFileAsync(
                        image,
                        "uploads/chat",
                        5 * 1024 * 1024,
                        new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" });

                    if (uploadResult.Succeeded)
                    {
                        imageUrl = uploadResult.FilePath;
                    }
                    else
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return BadRequest(new { success = false, message = uploadResult.ErrorMessage ?? "فشل تحميل الصورة." });
                        }
                        ModelState.AddModelError("image", uploadResult.ErrorMessage ?? "فشل تحميل الصورة.");
                        return RedirectToAction(nameof(Conversation), new { bookingId });
                    }
                }

                await _chatService.SendMessageAsync(
                    CurrentUserId,
                    booking.ClientId,
                    bookingId,
                    message,
                    imageUrl,
                    cancellationToken);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, imageUrl });
                }

                return RedirectToAction(
                    nameof(Conversation),
                    new { bookingId });
            }
        }
}
