using Shtbly.Services.Portfolio;

namespace Shtbly.Services.Portfolio
{
    public interface IFilePortfolioService
    {
        Task<FileUploadResult> UploadPortfolioMediaAsync(IFormFile file, string fileType);
        void DeleteFile(string? relativePath);
    }
}
