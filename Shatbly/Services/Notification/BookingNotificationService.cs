namespace Shatbly.Services.Notification
{
    public class BookingNotificationService(
    INotificationService notificationService,
    IEmailService emailService,
    ISmsService smsService) : IBookingNotificationService
    {
        public async Task NotifyBookingCreatedAsync(
            string userId,
            string email,
            string phoneNumber,
            int bookingId,
            CancellationToken cancellationToken = default)
        {
            var title = "Booking created";
            var message = $"Your booking #{bookingId} has been created and is pending confirmation.";

            await notificationService.CreateNotificationAsync(
                userId,
                title,
                message,
                NotificationType.BookingUpdate,
                bookingId,
                cancellationToken);
            await SendEmailIfAvailableAsync(email, title, message, cancellationToken);
            await SendSmsIfAvailableAsync(phoneNumber, message, cancellationToken);
        }

        public async Task NotifyBookingStatusChangedAsync(
    string userId,
    string email,
    string phoneNumber,
    int bookingId,
    OrderStatuses status,
    CancellationToken cancellationToken = default)
        {
            var (title, message) = status switch
            {
                OrderStatuses.Confirmed => (
                    "Booking accepted",
                    $"Your booking #{bookingId} has been accepted."),

                OrderStatuses.Cancelled => (
                    "Booking cancelled",
                    $"Your booking #{bookingId} has been cancelled."),

                OrderStatuses.Completed => (
                    "Booking completed",
                    $"Your booking #{bookingId} has been completed."),

                OrderStatuses.NoResponse => (
                    "Booking no response",
                    $"Your booking #{bookingId} was not accepted in time."),

                OrderStatuses.Rescheduled => (
                    "Booking rescheduled",
                    $"Your booking #{bookingId} has been rescheduled."),

                _ => (
                    "Booking updated",
                    $"Your booking #{bookingId} status is now {status}.")
            };

            await notificationService.CreateNotificationAsync(
                userId,
                title,
                message,
                NotificationType.BookingUpdate,
                bookingId,
                cancellationToken);

            if (status is OrderStatuses.Confirmed
                or OrderStatuses.Cancelled
                or OrderStatuses.Completed
                or OrderStatuses.NoResponse)
            {
                await SendEmailIfAvailableAsync(email, title, message, cancellationToken);
                await SendSmsIfAvailableAsync(phoneNumber, message, cancellationToken);
            }
        }


        private Task SendEmailIfAvailableAsync(string email, string subject, string body, CancellationToken cancellationToken)
        {
            return string.IsNullOrWhiteSpace(email)
                ? Task.CompletedTask
                : emailService.SendEmailAsync(email, subject, body, cancellationToken);
        }

        private Task SendSmsIfAvailableAsync(string phoneNumber, string message, CancellationToken cancellationToken)
        {
            return string.IsNullOrWhiteSpace(phoneNumber)
                ? Task.CompletedTask
                : smsService.SendSmsAsync(phoneNumber, message, cancellationToken);
        }
    }
}
