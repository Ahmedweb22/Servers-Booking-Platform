using Shtbly.Models;

namespace Shtbly.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IRepository<Booking> Bookings { get; private set; }
        public IRepository<WithdrawalRequest> WithdrawalRequests { get; private set; }
        public IRepository<WorkerProfile> WorkerProfiles { get; private set; }
        public IRepository<Avalability> Availabilities { get; private set; }
        public IRepository<UnAvalability> UnAvailabilities { get; private set; }
        public IRepository<PortfolioMedia> PortfolioMedia { get; private set; }
        public IRepository<ChatMessage> ChatMessages { get; private set; }
        public IRepository<Order> Orders { get;  set; }
        public IRepository<Notification> Notifications { get; private set; }
        public IRepository<Dipuste> Disputes { get; private set; }
        public IRepository<ServiceCategory> ServiceCategories { get; private set; }
        public IRepository<Wallet> Wallets { get; private set; }
        public IRepository<WalletTransaction> WalletTransactions { get; private set; }
        public IRepository<PromotionCode> PromotionCodes { get; private set; }
        public IRepository<Coupon> Coupons { get; private set; }
        public IRepository<Microsoft.AspNetCore.Identity.IdentityRole> Roles { get; private set; }
        public IRepository<Microsoft.AspNetCore.Identity.IdentityUserRole<string>> UserRoles { get; private set; }
        public IRepository<User> Users { get; private set; }
        public IRepository<WorkerService> WorkerServices { get; private set; }
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            Bookings = new Repository<Booking>(_context);
            WithdrawalRequests = new Repository<WithdrawalRequest>(_context);
            WorkerProfiles = new Repository<WorkerProfile>(_context);
            Availabilities = new Repository<Avalability>(_context);
            UnAvailabilities = new Repository<UnAvalability>(_context);
            PortfolioMedia = new Repository<PortfolioMedia>(_context);
            Notifications = new Repository<Notification>(_context);
            ChatMessages = new Repository<ChatMessage>(_context);
            Orders = new Repository<Order>(_context);
            Disputes = new Repository<Dipuste>(_context);
            ServiceCategories = new Repository<ServiceCategory>(_context);
            Wallets = new Repository<Wallet>(_context);
            WalletTransactions = new Repository<WalletTransaction>(_context);
            PromotionCodes = new Repository<PromotionCode>(_context);
            Coupons = new Repository<Coupon>(_context);
            Roles = new Repository<Microsoft.AspNetCore.Identity.IdentityRole>(_context);
            UserRoles = new Repository<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>(_context);
            Users = new Repository<User>(_context);
            WorkerServices = new Repository<WorkerService>(_context);
            ChatMessages = new Repository<ChatMessage>(_context);
        }

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
