using Shtbly.Models;
using Shtbly.ViewModels;

namespace Shtbly.Services.WithdrawalService
{
    public interface IWithdrawalService
    {
        Task<ServiceResult> CreateRequestAsync(int workerId, decimal amount);
        Task<IReadOnlyList<WithdrawalListVM>> GetRequestsAsync(int workerId);
        Task<ServiceResult> ValidateWithdrawalAsync(int workerId, decimal amount);
        Task<IReadOnlyList<WithdrawalRequest>> GetAllRequestsAsync();
        Task<ServiceResult> ApproveRequestAsync(int requestId);
        Task<ServiceResult> RejectRequestAsync(int requestId);
    }
}
