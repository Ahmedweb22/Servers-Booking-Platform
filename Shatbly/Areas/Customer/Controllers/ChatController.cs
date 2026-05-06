using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.Chat;
using Shatbly.ViewModels;

namespace Shatbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_CUSTOMER},{SD.ROLE_ADMIN}")]
    public class ChatController(IChatService chatService) : Controller
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

            var messages = await chatService.GetConversationAsync(
                currentUserId,
                receiverId,
                bookingId,
                cancellationToken);

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
            string message,
            CancellationToken cancellationToken)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(senderId))
            {
                return Unauthorized();
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                await chatService.SendMessageAsync(
                    senderId,
                    receiverId,
                    bookingId,
                    message,
                    cancellationToken);
            }

            return RedirectToAction(nameof(Index), new
            {
                bookingId,
                receiverId
            });
        }
    }
}
