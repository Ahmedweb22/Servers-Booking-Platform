
namespace Shatbly.Models
{
    public enum OrderStatuses
    {
        Pending,
        Confirmed,
        Completed,
        Cancelled,
        NoResponse,
        Rescheduled
    }

    public enum BookingTypes
    {
        Scheduled,
        Instant
    }

    public enum PaymentMethods
    {
        Cash,
        Card,
        Wallet
    }

    public enum PaymentStatuses
    {
        Pending,
        Paid,
        Failed,
        Refunded
    }

    public enum RecurrencePatterns
    {
        None,
        Daily,
        Weekly,
        Monthly
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        public string? WorkerId { get; set; }

        [Required]
        public OrderStatuses Status { get; set; } = OrderStatuses.Pending;

        [Required]
        public BookingTypes BookingType { get; set; } = BookingTypes.Scheduled;

        [Required]
        public DateTime ScheduledAt { get; set; } = DateTime.UtcNow.AddDays(1);

        [Range(1, 8)]
        public int DurationHours { get; set; } = 1;

        [Required, StringLength(120)]
        public string AddressLabel { get; set; } = "Home";

        [Required, StringLength(250)]
        public string AddressLine { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public PaymentMethods PaymentMethod { get; set; } = PaymentMethods.Cash;

        [Required]
        public PaymentStatuses PaymentStatus { get; set; } = PaymentStatuses.Pending;

        [Required]
        public RecurrencePatterns RecurrencePattern { get; set; } = RecurrencePatterns.None;

        public decimal ServicePrice { get; set; }
        public decimal ConvenienceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime? WorkerResponseDeadlineUtc { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public ServiceCategory? Service { get; set; }
        public User? Worker { get; set; }

        //public static implicit operator Order(Order v)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
