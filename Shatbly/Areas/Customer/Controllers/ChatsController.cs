using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.Chat;
using Shatbly.Services.Notification;
using Shatbly.Services.File_Service;
using System.Security.Claims;

namespace Shatbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]
    //IChatService chatService
    public class ChatsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IChatService _chatService;
        private readonly IFileService _fileService;

        public ChatsController(
            ApplicationDbContext context,
            INotificationService notificationService,
            IChatService chatService,
            IFileService fileService)
        {
            _context = context;
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

            var booking = await _context.Bookings
                .Include(x => x.Client)
                .Include(x => x.Worker)
                    .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.Id == bookingId,
                    cancellationToken);

            if (booking == null)
                return NotFound();

            // العميل لا يفتح إلا حجوزه
            if (booking.ClientId != CurrentUserId)
                return Forbid();

            var messages = await _context.ChatMessages
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x => x.BookingId == bookingId)
                .OrderBy(x => x.SentAt)
                .ToListAsync(cancellationToken);

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

            await _context.SaveChangesAsync(cancellationToken);

            var users = new[] { CurrentUserId, booking.Worker.UserId }.OrderBy(x => x).ToArray();
            ViewBag.ConversationKey = $"booking-{booking.Id}-chat-{users[0]}-{users[1]}";

            var vm = new ChatCustomerViewModel
            {
                BookingId = booking.Id,
                WorkerId = booking.Worker.UserId,
                WorkerName = booking.Worker.User.UserName,
                Messages = messages
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

            var booking = await _context.Bookings
                .Include(x => x.Worker)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.Id == bookingId,
                    cancellationToken);

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
