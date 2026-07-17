using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shtbly.Models
{
    public enum ReviewDirection
    {
        ClientToWorker,
        WorkerToClient
    }
    public class Review
    {

        [Key]
        public int Id { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string Comment { get; set; }

        [Required]
        public ReviewDirection Direction { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? BeforeImageUrl { get; set; }
        public string? AfterImageUrl { get; set; }
        [Required]
        public int OrderId { get; set; }
        [ForeignKey(nameof(BookingId))]
        public Order Order { get; set; }
        [Required]
        public int BookingId { get; set; }
        [ForeignKey(nameof(BookingId))]
        public Booking Booking { get; set; }
        [Required]
        public string ReviewerId { get; set; }

        [ForeignKey(nameof(ReviewerId))]
        public User Reviewer { get; set; }
        public string RevieweeId { get; set; }
        [ForeignKey(nameof(RevieweeId))]
        public User Reviewee { get; set; }

        [Required]
        public int CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public ServiceCategory Category { get; set; }

        public bool IsApproved { get; set; } = false;

        public int? WorkerProfileId { get; set; }
        [ForeignKey(nameof(WorkerProfileId))]
        public WorkerProfile? WorkerProfile { get; set; }
    }
}
