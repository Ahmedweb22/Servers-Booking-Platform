namespace Shtbly.ViewModels
{
    public class ChatWorkerViewModel
    {
        public int BookingId { get; set; }

        public string ClientName { get; set; }

        public string ClientId { get; set; }

        public string? ClientProfilePictureUrl { get; set; }

        public string? WorkerProfilePictureUrl { get; set; }

        public List<ChatMessage> Messages { get; set; } = [];
    }
}
