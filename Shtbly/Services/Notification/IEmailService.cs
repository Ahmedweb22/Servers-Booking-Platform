namespace Shtbly.Services.Notification
{
    public interface IEmailService
    {
        Task SendEmailAsync(
            string to,
            string subject,
            string body,
            CancellationToken cancellationToken = default);
    }
}
