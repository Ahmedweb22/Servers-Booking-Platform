using Shatbly.ViewModels;

namespace Shatbly.UnitOfWork
{
    public interface IUnitOfWork
    {
        IRepository<WorkerProfile> WorkerProfiles { get; }
        IRepository<Avalability> Availabilities { get; }
        IRepository<UnAvalability> UnAvailabilities { get; }
        IRepository<PortfolioMedia> PortfolioMedia { get; }

        IRepository<Booking> Bookings { get; }
        IRepository<WithdrawalRequest> WithdrawalRequests { get; }

        Task<int> CommitAsync();
    }
}
