using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shtbly.Services.AI;

namespace Shtbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]
    public class ServiceChatController : Controller
    {
        private readonly IChatAiService _chatService;

        public ServiceChatController(IChatAiService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest();

            try
            {
                var reply = await _chatService.AskAsync(request.Message, request.History ?? []);
                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                // Log the real error
                Console.WriteLine($"[ChatAI Error] {ex.Message}");
                return StatusCode(503, new { reply = "عذراً، المساعد غير متاح حالياً. حاول بعد قليل." });
            }
        }
        public class ChatRequestDto
        {
            public string Message { get; set; } = "";
            public List<Services.AI.ChatMessage> History { get; set; } = [];
        }
    }
}
