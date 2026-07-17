using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shtbly.Services.Chat;
using Shtbly.Services.Notification;
using Shtbly.Services.File_Service;
using System.Security.Claims;

namespace Shtbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]
    //IChatService chatService
    public class ChatsController : Controller
    {
        private readonly Shtbly.UnitOfWork.IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IChatService _chatService;
        private readonly IFileService _fileService;

        public ChatsController(
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

            // العميل لا يفتح إلا حجوزه
            if (booking.ClientId != CurrentUserId)
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

            var users = new[] { CurrentUserId, booking.Worker.UserId }.OrderBy(x => x).ToArray();
            ViewBag.ConversationKey = $"booking-{booking.Id}-chat-{users[0]}-{users[1]}";

            var vm = new ChatCustomerViewModel
            {
                BookingId = booking.Id,
                WorkerId = booking.Worker.UserId,
                WorkerName = booking.Worker.User.UserName,
                Messages = messages,
                CustomerProfilePictureUrl = booking.Client.ProfilePicture,
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

            var booking = await _unitOfWork.Bookings.GetOneAsync(
                expression: x => x.Id == bookingId,
                includes: new System.Linq.Expressions.Expression<System.Func<Booking, object>>[] 
                { 
                    x => x.Worker!, 
                    x => x.Worker!.User! 
                },
                tracking: false);

            if (booking == null)
                return NotFound();

            if (booking.ClientId != CurrentUserId)
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
                booking.Worker.UserId,
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
