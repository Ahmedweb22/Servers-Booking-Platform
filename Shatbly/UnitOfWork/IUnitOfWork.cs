using Shatbly.Models;

namespace Shatbly.UnitOfWork
{
    public interface IUnitOfWork
    {
        IRepository<Booking> Bookings { get; }
        IRepository<WithdrawalRequest> WithdrawalRequests { get; }
        IRepository<WorkerProfile> WorkerProfiles { get; }
        IRepository<Avalability> Availabilities { get; }
        IRepository<UnAvalability> UnAvailabilities { get; }
        IRepository<PortfolioMedia> PortfolioMedia { get; }
        IRepository<ChatMessage> ChatMessages { get; }
        IRepository<Notification> Notifications { get; }
        public IRepository<Order> Orders { get; }
        Task<int> CommitAsync();
    }
}
