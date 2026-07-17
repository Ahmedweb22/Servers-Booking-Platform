using Shtbly.Models;

namespace Shtbly.ViewModels
{
    public class ChatViewModel
    {
        public int BookingId { get; set; }
        public string ReceiverId { get; set; } = string.Empty;
        public IReadOnlyList<ChatMessage> Messages { get; set; } = [];
    }
}
