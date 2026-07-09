using Shatbly.Models;

namespace Shatbly.UnitOfWork
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
        }

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
