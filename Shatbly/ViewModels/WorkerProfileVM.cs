namespace Shatbly.ViewModels
{
    public class WorkerProfileVM
    {
        public int Id { get; set; }

        public string WorkerName { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        public decimal RatingAvg { get; set; }

        public int RatingCount { get; set; }

        public bool IsVerified { get; set; }

        public bool IsAvailable { get; set; }

        public bool AcceptsOnline { get; set; }

        public string? CVPath { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
