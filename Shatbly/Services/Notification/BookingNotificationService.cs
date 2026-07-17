namespace Shtbly.Services.Notification
{
    public class BookingNotificationService(
    INotificationService notificationService,
    IEmailService emailService,
    ISmsService smsService,
    IStringLocalizer<BookingNotificationService> localizer) : IBookingNotificationService
    {
        public async Task NotifyBookingCreatedAsync(
            string userId,
            string email,
            string phoneNumber,
            int bookingId,
            CancellationToken cancellationToken = default)
        {
            var title = localizer["BookingCreatedTitle"].Value;
            var message = string.Format(localizer["BookingCreatedMessage"].Value, bookingId);

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
                    localizer["BookingAcceptedTitle"].Value,
                    string.Format(localizer["BookingAcceptedMessage"].Value, bookingId)),

                OrderStatuses.Cancelled => (
                    localizer["BookingCancelledTitle"].Value,
                    string.Format(localizer["BookingCancelledMessage"].Value, bookingId)),

                OrderStatuses.Completed => (
                    localizer["BookingCompletedTitle"].Value,
                    string.Format(localizer["BookingCompletedMessage"].Value, bookingId)),

                OrderStatuses.NoResponse => (
                    localizer["BookingNoResponseTitle"].Value,
                    string.Format(localizer["BookingNoResponseMessage"].Value, bookingId)),

                OrderStatuses.Rescheduled => (
                    localizer["BookingRescheduledTitle"].Value,
                    string.Format(localizer["BookingRescheduledMessage"].Value, bookingId)),

                _ => (
                    localizer["BookingUpdatedTitle"].Value,
                    string.Format(localizer["BookingUpdatedMessage"].Value, bookingId, status))
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
