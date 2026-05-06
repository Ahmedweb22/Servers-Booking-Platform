using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.Chat;
using System.Security.Claims;

namespace Shatbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_CUSTOMER},{SD.ROLE_ADMIN}")]
    public class ChatsController(IChatService chatService) : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
            string receiverId,
            int bookingId,
            string message,
            CancellationToken cancellationToken)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(senderId))
            {
                return Unauthorized();
            }

            await chatService.SendMessageAsync(
                senderId,
                receiverId,
                bookingId,
                message,
                cancellationToken);

            return Ok("Message sent");
        }
    }
}
