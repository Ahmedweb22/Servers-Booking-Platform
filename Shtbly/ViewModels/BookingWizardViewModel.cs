using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shtbly.Models;

namespace Shtbly.ViewModels
{
    public enum BookingTypes
    {
        None,
        Scheduled,
        Instant
    }

    public class BookingWizardViewModel
    {
        [Required(ErrorMessage = "Choose a service.")]
        public int? ServiceId { get; set; }

        [Required(ErrorMessage = "Choose a worker.")]
        public string? WorkerId { get; set; }

        [Required(ErrorMessage = "Choose a booking type.")]
        public BookingTypes BookingType { get; set; } = BookingTypes.Scheduled;

        [Required(ErrorMessage = "Choose a date.")]
        public string SelectedDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose a time.")]
        public string SelectedTime { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required, Phone, StringLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string AddressLabel { get; set; } = "Home";

        [Required, StringLength(250)]
        public string AddressLine { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public PaymentMethods PaymentMethod { get; set; } = PaymentMethods.Cash;

        [Required]
        public RecurrencePatterns RecurrencePattern { get; set; } = RecurrencePatterns.None;

        public IReadOnlyList<SelectListItem> ServiceOptions { get; set; } = [];
        public IReadOnlyList<SelectListItem> WorkerOptions { get; set; } = [];
        public IReadOnlyList<SelectListItem> PaymentOptions { get; set; } = [];
        public IReadOnlyList<SelectListItem> RecurrenceOptions { get; set; } = [];
        public IReadOnlyList<SelectListItem> AddressPresets { get; set; } = [];
        public string AvailabilityJson { get; set; } = "{}";
        public string WorkerDetailsJson { get; set; } = "{}";
        public string ServiceDetailsJson { get; set; } = "{}";
        public int? PromoCodeId { get; set; }
        public string? PromoCodeText { get; set; }
        public int? CouponId { get; set; }
        public string? CouponCode { get; set; }
        [Required(ErrorMessage = "Choose duration.")]
        [Range(1, 8, ErrorMessage = "Duration must be between 1 and 8 hours.")]
        public int DurationHours { get; set; } = 1;
        public string SelectedServiceName { get; set; } = string.Empty;
        public string SelectedWorkerName { get; set; } = string.Empty;
        public decimal ServicePrice { get; set; }
        public decimal ConvenienceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsWorkerLocked { get; set; }
    }
}
