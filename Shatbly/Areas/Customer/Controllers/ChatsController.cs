using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.Chat;
using Shatbly.Services.Notification;
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

        public ChatsController(
            ApplicationDbContext context,
            INotificationService notificationService , IChatService chatService)
        {
            _context = context;
            _notificationService = notificationService;
            _chatService = chatService;
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
      string message,
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

            await _chatService.SendMessageAsync(
                CurrentUserId,
                booking.Worker.UserId,
                bookingId,
                message,
                cancellationToken);

            return RedirectToAction(
                nameof(Conversation),
                new { bookingId });
        }
    }
}
