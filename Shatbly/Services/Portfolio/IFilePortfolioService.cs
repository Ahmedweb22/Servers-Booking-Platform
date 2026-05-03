using Shatbly.Services.Portfolio;

namespace Shatbly.Services.Portfolio
{
    public interface IFilePortfolioService
    {
        Task<FileUploadResult> UploadPortfolioMediaAsync(IFormFile file, string fileType);
        void DeleteFile(string? relativePath);
    }
}
