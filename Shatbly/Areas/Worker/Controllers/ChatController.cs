using global::Shatbly.Services.Notification;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Security.Claims;

namespace Shatbly.Areas.Worker.Controllers
    {
        [Area(SD.WORKER_AREA)]
        [Authorize(Roles = $"{SD.ROLE_WORKER},{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN}")]
        public class ChatController : Controller
        {
            private readonly ApplicationDbContext _context;
            private readonly INotificationService _notificationService;

            public ChatController(
                ApplicationDbContext context,
                INotificationService notificationService)
            {
                _context = context;
                _notificationService = notificationService;
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

                var workerProfile = await _context.WorkerProfiles
                    .FirstOrDefaultAsync(
                        x => x.UserId == CurrentUserId,
                        cancellationToken);

                if (workerProfile == null)
                    return Unauthorized();

                var booking = await _context.Bookings
                    .Include(x => x.Client)
                    .Include(x => x.Worker)
                    .FirstOrDefaultAsync(
                        x => x.Id == bookingId,
                        cancellationToken);

                if (booking == null)
                    return NotFound();

                if (booking.WorkerId != workerProfile.Id)
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

                var vm = new ChatWorkerViewModel
                {
                    BookingId = booking.Id,
                    ClientId = booking.ClientId,
                    ClientName = booking.Client.UserName,
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

                if (string.IsNullOrWhiteSpace(message))
                    return RedirectToAction(
                        nameof(Conversation),
                        new { bookingId });

                var workerProfile = await _context.WorkerProfiles
                    .FirstOrDefaultAsync(
                        x => x.UserId == CurrentUserId,
                        cancellationToken);

                if (workerProfile == null)
                    return Unauthorized();

                var booking = await _context.Bookings
                    .Include(x => x.Client)
                    .FirstOrDefaultAsync(
                        x => x.Id == bookingId,
                        cancellationToken);

                if (booking == null)
                    return NotFound();

                if (booking.WorkerId != workerProfile.Id)
                    return Forbid();

                var chatMessage = new ChatMessage
                {
                    BookingId = bookingId,
                    SenderId = CurrentUserId,
                    ReceiverId = booking.ClientId,
                    Message = message,
                    SentAt = DateTime.UtcNow
                };

                _context.ChatMessages.Add(chatMessage);

                await _notificationService.CreateNotificationAsync(
                    booking.ClientId,
                    "New Message",
                    message,
                    NotificationType.Message,
                    bookingId,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                return RedirectToAction(
                    nameof(Conversation),
                    new { bookingId });
            }
        }
}
