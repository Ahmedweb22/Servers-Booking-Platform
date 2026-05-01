namespace Shatbly.Services.Portfolio
{
    public interface IPortfolioService
    {
        Task<IReadOnlyList<PortfolioVM>> GetWorkerPortfolioAsync(string userId);
        Task<PortfolioOperationResult> UploadMediaAsync(string userId, UploadPortfolioVM model);
        Task<PortfolioOperationResult> DeleteMediaAsync(string userId, int mediaId);
    }
}
