using Shtbly.Models;

namespace Shtbly.UnitOfWork
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
        IRepository<Dipuste> Disputes { get; }
        IRepository<ServiceCategory> ServiceCategories { get; }
        IRepository<Wallet> Wallets { get; }
        IRepository<WalletTransaction> WalletTransactions { get; }
        IRepository<PromotionCode> PromotionCodes { get; }
        IRepository<Coupon> Coupons { get; }
        IRepository<Microsoft.AspNetCore.Identity.IdentityRole> Roles { get; }
        IRepository<Microsoft.AspNetCore.Identity.IdentityUserRole<string>> UserRoles { get; }
        IRepository<User> Users { get; }
        IRepository<WorkerService> WorkerServices { get; }
        Task<int> CommitAsync();
    }
}
