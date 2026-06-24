using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shatbly.DataAccess;
using Shatbly.Services.Chat;
using Shatbly.ViewModels;
using IChatService = Shatbly.Services.Chat.IChatService;

namespace Shatbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]
    public class ChatController(IChatService chatService, ApplicationDbContext context, Shatbly.Services.File_Service.IFileService fileService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(
            int bookingId,
            string receiverId,
            CancellationToken cancellationToken)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized();
            }

            var workerProfile = await context.WorkerProfiles.FirstOrDefaultAsync(wp => wp.UserId == receiverId, cancellationToken);
            
            // Verify if the passed bookingId is a valid booking in the database for this specific client and worker
            var bookingExists = false;
            if (workerProfile != null)
            {
                bookingExists = await context.Bookings.AnyAsync(b => 
                    b.Id == bookingId && 
                    b.ClientId == currentUserId && 
                    b.WorkerId == workerProfile.Id, 
                    cancellationToken);
            }

            if (!bookingExists)
            {
                // If the booking ID is not valid or doesn't match this client-worker pair, 
                // look for an existing booking between this client and this worker
                if (workerProfile != null)
                {
                    var existingBooking = await context.Bookings
                        .Where(b => b.ClientId == currentUserId && b.WorkerId == workerProfile.Id)
                        .OrderByDescending(b => b.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existingBooking != null)
                    {
                        bookingId = existingBooking.Id;
                    }
                    else
                    {
                        // No booking exists yet
                        bookingId = 0;
                    }
                }
                else
                {
                    bookingId = 0;
                }
            }

            IReadOnlyList<ChatMessage> messages = [];
            if (bookingId > 0)
            {
                messages = await chatService.GetConversationAsync(
                    currentUserId,
                    receiverId,
                    bookingId,
                    cancellationToken);

                var users = new[] { currentUserId, receiverId }.OrderBy(x => x).ToArray();
                ViewBag.ConversationKey = $"booking-{bookingId}-chat-{users[0]}-{users[1]}";
            }

            return View(new ChatViewModel
            {
                BookingId = bookingId,
                ReceiverId = receiverId,
                Messages = messages
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
            int bookingId,
            string receiverId,
            string? message,
            IFormFile? image,
            CancellationToken cancellationToken)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(senderId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(receiverId))
            {
                return BadRequest("لا يمكن إرسال الرسالة، معرف المستلم مفقود.");
            }

            if (bookingId <= 0)
            {
                return BadRequest("لا يوجد حجز نشط لبدء المحادثة.");
            }

            string? imageUrl = null;
            if (image != null && image.Length > 0)
            {
                var uploadResult = await fileService.UploadFileAsync(
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
                    return BadRequest(uploadResult.ErrorMessage ?? "فشل تحميل الصورة.");
                }
            }

            if (!string.IsNullOrWhiteSpace(message) || !string.IsNullOrWhiteSpace(imageUrl))
            {
                await chatService.SendMessageAsync(
                    senderId,
                    receiverId,
                    bookingId,
                    message,
                    imageUrl,
                    cancellationToken);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, imageUrl });
            }

            return RedirectToAction(nameof(Index), new
            {
                bookingId,
                receiverId
            });
        }
    }
}
