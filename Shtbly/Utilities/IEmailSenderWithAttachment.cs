using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Shtbly.Utilities
{
    public interface IEmailSenderWithAttachment : IEmailSender
    {
        Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, string filePath);
    }
}
