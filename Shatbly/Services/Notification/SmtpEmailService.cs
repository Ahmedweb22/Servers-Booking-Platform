using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Shatbly.Services.Notification
{
    public class SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
    {
        private readonly SmtpOptions _options = options.Value;

        public async Task SendEmailAsync(
            string to,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromEmail))
            {
                logger.LogWarning("SMTP settings are incomplete. Email to {Recipient} was skipped.", to);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(to);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
            }

            using var registration = cancellationToken.Register(client.SendAsyncCancel);
            await client.SendMailAsync(message, cancellationToken);
        }
    }

}
