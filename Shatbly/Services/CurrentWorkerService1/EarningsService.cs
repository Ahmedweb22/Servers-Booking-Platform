using Shatbly.Models;
using Shatbly.UnitOfWork;
using Shatbly.ViewModels;

namespace Shatbly.Services.CurrentWorkerService1
{
    public class EarningsService : IEarningsService
    {
        private const string PendingWithdrawalStatus = "Pending";
        private const string ApprovedWithdrawalStatus = "Approved";

        private readonly IUnitOfWork _unitOfWork;

        public EarningsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<EarningsDashboardVM> GetDashboardAsync(int workerId)
        {
            var completedBookings = await _unitOfWork.Bookings.GetAsync(
                x => x.WorkerId == workerId && x.Status == BookingStatus.Completed,
                tracking: false);

            var withdrawals = await _unitOfWork.WithdrawalRequests.GetAsync(
                x => x.WorkerId == workerId,
                tracking: false);

            var now = DateTime.UtcNow;

            var totalEarnings = completedBookings.Sum(x => x.TotalPrice);

            var monthlyEarnings = completedBookings
                .Where(x => x.ScheduledAt.Month == now.Month && x.ScheduledAt.Year == now.Year)
                .Sum(x => x.TotalPrice);

            var unavailableBalance = withdrawals
                .Where(x => x.Status == PendingWithdrawalStatus || x.Status == ApprovedWithdrawalStatus)
                .Sum(x => x.Amount);

            return new EarningsDashboardVM
            {
                TotalEarnings = totalEarnings,
                MonthlyEarnings = monthlyEarnings,
                PendingBalance = totalEarnings - unavailableBalance,
                RecentTransactions = completedBookings
                    .OrderByDescending(x => x.ScheduledAt)
                    .Take(10)
                    .Select(x => new EarningTransactionVM
                    {
                        Amount = x.TotalPrice,
                        Date = x.ScheduledAt,
                        Status = x.Status.ToString()
                    })
                    .ToList()
            };
        }
    }
}
