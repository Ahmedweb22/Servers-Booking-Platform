namespace Shatbly.Services.Notification
{
    public interface ISmsService
    {
        Task SendSmsAsync(
            string phoneNumber,
            string message,
            CancellationToken cancellationToken = default);
    }

}
