using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using Shtbly.Services.Notification;

namespace Shtbly.Utilities
{
    public class EmailSender(IOptions<SmtpOptions> options, ILogger<EmailSender> logger) : IEmailSenderWithAttachment
    {
        private readonly SmtpOptions _options = options.Value;

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await SendEmailWithAttachmentAsync(email, subject, htmlMessage, null);
        }

        public async Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, string filePath)
        {
            if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromEmail))
            {
                logger.LogWarning("SMTP settings are incomplete. Identity email to {Recipient} was skipped.", email);
                return;
            }

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
            }

            using var mailMessage = new MailMessage(_options.FromEmail, email, subject, htmlMessage)
            {
                IsBodyHtml = true
            };

            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
            {
                mailMessage.Attachments.Add(new Attachment(filePath));
            }

            await client.SendMailAsync(mailMessage);
        }
    }
}
