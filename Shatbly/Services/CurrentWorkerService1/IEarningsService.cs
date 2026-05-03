namespace Shatbly.Services.CurrentWorkerService1
{
    public interface IEarningsService
    {
        Task<EarningsDashboardVM> GetDashboardAsync(int workerId);
    }
}
