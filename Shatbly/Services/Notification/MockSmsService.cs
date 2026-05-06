namespace Shatbly.Services.Notification
{
    public class MockSmsService(ILogger<MockSmsService> logger) : ISmsService
    {
        public Task SendSmsAsync(
            string phoneNumber,
            string message,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Mock SMS sent to {PhoneNumber}: {Message}", phoneNumber, message);
            return Task.CompletedTask;
        }
    }

}
