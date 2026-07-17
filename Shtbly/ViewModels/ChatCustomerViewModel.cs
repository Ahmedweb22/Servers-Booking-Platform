namespace Shtbly.ViewModels
{
    public class ChatCustomerViewModel
    {
        public int BookingId { get; set; }

        public string WorkerId { get; set; }

        public string WorkerName { get; set; }

        public string? CustomerProfilePictureUrl { get; set; }

        public string? WorkerProfilePictureUrl { get; set; }

        public List<ChatMessage> Messages { get; set; } = [];
    }
}
