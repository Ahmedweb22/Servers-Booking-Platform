namespace Shatbly.Models
{
    public class WithdrawalRequest
    {
        public int Id { get; set; }

        public int WorkerId { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public WorkerProfile Worker { get; set; }
    }

}