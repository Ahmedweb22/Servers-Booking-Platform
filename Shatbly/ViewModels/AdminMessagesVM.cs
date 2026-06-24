using Shatbly.Models;
using System.Collections.Generic;

namespace Shatbly.ViewModels
{
    public class AdminMessagesVM
    {
        public List<WorkerProfile> PendingWorkers { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<Notification> SentNotifications { get; set; } = new();
        public List<Notification> IncomingMessages { get; set; } = new();

        // Form properties for sending a direct message
        public string? TargetUserId { get; set; }
        public string? MessageTitle { get; set; }
        public string? MessageBody { get; set; }
        public NotificationType MessageType { get; set; } = NotificationType.Message;
    }
}
