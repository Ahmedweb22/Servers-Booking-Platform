using Shatbly.Models;

namespace Shatbly.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IRepository<Booking> Bookings { get; private set; }
        public IRepository<Shatbly.Models.WithdrawalRequest> WithdrawalRequests { get; private set; }
        public IRepository<WorkerProfile> WorkerProfiles { get; private set; }
        public IRepository<Avalability> Availabilities { get; private set; }
        public IRepository<UnAvalability> UnAvailabilities { get; private set; }
        public IRepository<PortfolioMedia> PortfolioMedia { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            Bookings = new Repository<Booking>(_context);
            WithdrawalRequests = new Repository<Shatbly.Models.WithdrawalRequest>(_context);
            WorkerProfiles = new Repository<WorkerProfile>(_context);
            Availabilities = new Repository<Avalability>(_context);
            UnAvailabilities = new Repository<UnAvalability>(_context);
            PortfolioMedia = new Repository<PortfolioMedia>(_context);
        }

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }

}
