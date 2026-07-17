using Shtbly.Models;
using Shtbly.ViewModels;

namespace Shtbly.Services.BookingSystem;

public interface IBookingSystemService
{
    Task<BookingWizardViewModel> BuildCreateViewModelAsync(BookingWizardViewModel? model = null);
    Task<BookingCreateResult> CreateAsync(BookingWizardViewModel model);
    Task<BookingDetailsViewModel?> GetDetailsAsync(int id);
    Task<BookingActionResult> RescheduleAsync(int id, string scheduledAt);
    Task<BookingActionResult> CancelAsync(int id, string? cancellationReason);
    Task<bool> MarkAsPaidAsync(int bookingId, string paidByUserId);
    Task<bool> AddReviewAsync(Review review);
    Task<bool> RaiseDisputeAsync(int bookingId, string reason, string raisedById);
    Task<List<Order>> GetCustomerOrdersAsync(string userId);
    Task<Dictionary<string, List<string>>> GetAvailableSlotsByDateAsync(string workerId, int? ignoreOrderId = null);
    Task<bool> UpdateWorkerOrderStatusAsync(int orderId, OrderStatuses status, string currentUserId, bool isAr, string workerName);
}
