using Shatbly.Services.CurrentWorkerService1;
using Shatbly.ViewModels;

namespace Shatbly.Services.WithdrawalService
{
    public class WithdrawalService : IWithdrawalService
    {
        private const string PendingStatus = "Pending";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IEarningsService _earningsService;

        public WithdrawalService(IUnitOfWork unitOfWork, IEarningsService earningsService)
        {
            _unitOfWork = unitOfWork;
            _earningsService = earningsService;
        }

        public async Task<ServiceResult> CreateRequestAsync(int workerId, decimal amount)
        {
            var validation = await ValidateWithdrawalAsync(workerId, amount);

            if (!validation.Succeeded)
            {
                return validation;
            }

            var request = new WithdrawalRequest
            {
                WorkerId = workerId,
                Amount = amount,
                Status = PendingStatus,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.WithdrawalRequests.CreateAsync(request);
            await _unitOfWork.CommitAsync();

            return ServiceResult.Success();
        }

        public async Task<IReadOnlyList<WithdrawalListVM>> GetRequestsAsync(int workerId)
        {
            var requests = await _unitOfWork.WithdrawalRequests.GetAsync(
                x => x.WorkerId == workerId,
                tracking: false);

            return requests
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new WithdrawalListVM
                {
                    Id = x.Id,
                    Amount = x.Amount,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt
                })
                .ToList();
        }

        public async Task<ServiceResult> ValidateWithdrawalAsync(int workerId, decimal amount)
        {
            if (amount <= 0)
            {
                return ServiceResult.Failure("Withdrawal amount must be greater than zero.");
            }

            var dashboard = await _earningsService.GetDashboardAsync(workerId);

            if (amount > dashboard.PendingBalance)
            {
                return ServiceResult.Failure("You cannot withdraw more than your available balance.");
            }

            return ServiceResult.Success();
        }
    }
}
