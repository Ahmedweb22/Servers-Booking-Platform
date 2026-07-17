namespace Shtbly.ViewModels
{
    public class BookingDetailsViewModel
    {
        public Order Booking { get; set; } = null!;
        public decimal RefundPreview { get; set; }
        public bool CanReschedule { get; set; }
        public bool CanCancel { get; set; }
        public bool HasReview { get; set; }
        public Models.Review? Review { get; set; }
    }
}
