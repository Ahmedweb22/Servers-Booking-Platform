namespace Shatbly.Services.BookingSystem;

public interface IBookingSystemService
{
    Task<BookingWizardViewModel> BuildCreateViewModelAsync(BookingWizardViewModel? model = null);
    Task<BookingCreateResult> CreateAsync(BookingWizardViewModel model);
    Task<BookingDetailsViewModel?> GetDetailsAsync(int id);
    Task<BookingActionResult> RescheduleAsync(int id, string scheduledAt);
    Task<BookingActionResult> CancelAsync(int id, string? cancellationReason);
}
