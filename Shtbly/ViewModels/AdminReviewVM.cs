using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Shtbly.ViewModels
{
    public class AdminReviewVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a booking.")]
        public int BookingId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Comment is required.")]
        [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string Comment { get; set; } = string.Empty;

        public IEnumerable<SelectListItem>? Bookings { get; set; }
    }
}
