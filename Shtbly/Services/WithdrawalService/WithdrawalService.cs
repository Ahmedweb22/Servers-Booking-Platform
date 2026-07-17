using Shtbly.Services.CurrentWorkerService1;
using Shtbly.ViewModels;

namespace Shtbly.Services.WithdrawalService
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

        public async Task<IReadOnlyList<WithdrawalRequest>> GetAllRequestsAsync()
        {
            return await _unitOfWork.WithdrawalRequests.GetAsync(
                includes: new System.Linq.Expressions.Expression<System.Func<WithdrawalRequest, object>>[]
                {
                    x => x.Worker,
                    x => x.Worker.User
                },
                tracking: false);
        }

        public async Task<ServiceResult> ApproveRequestAsync(int requestId)
        {
            var request = await _unitOfWork.WithdrawalRequests.GetOneAsync(
                x => x.Id == requestId,
                tracking: true);

            if (request == null)
            {
                return ServiceResult.Failure("Withdrawal request not found.");
            }

            if (request.Status != PendingStatus)
            {
                return ServiceResult.Failure("Only pending requests can be approved.");
            }

            request.Status = "Approved";
            _unitOfWork.WithdrawalRequests.Update(request);
            await _unitOfWork.CommitAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RejectRequestAsync(int requestId)
        {
            var request = await _unitOfWork.WithdrawalRequests.GetOneAsync(
                x => x.Id == requestId,
                tracking: true);

            if (request == null)
            {
                return ServiceResult.Failure("Withdrawal request not found.");
            }

            if (request.Status != PendingStatus)
            {
                return ServiceResult.Failure("Only pending requests can be rejected.");
            }

            request.Status = "Rejected";
            _unitOfWork.WithdrawalRequests.Update(request);
            await _unitOfWork.CommitAsync();

            return ServiceResult.Success();
        }
    }
}
