namespace Shtbly.Services.Notification
{
    public interface IBookingNotificationService
    {
        Task NotifyBookingCreatedAsync(
            string userId,
            string email,
            string phoneNumber,
            int bookingId,
            CancellationToken cancellationToken = default);

        Task NotifyBookingStatusChangedAsync(
            string userId,
            string email,
            string phoneNumber,
            int bookingId,
            OrderStatuses status,
            CancellationToken cancellationToken = default);
    }
}
