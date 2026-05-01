namespace Shatbly.Services.WithdrawalService
{
    public interface IWithdrawalService
    {
        Task<ServiceResult> CreateRequestAsync(int workerId, decimal amount);
        Task<IReadOnlyList<WithdrawalListVM>> GetRequestsAsync(int workerId);
        Task<ServiceResult> ValidateWithdrawalAsync(int workerId, decimal amount);
    }
}
