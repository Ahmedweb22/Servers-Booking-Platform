using Shatbly.Services.Portfolio;
namespace Shatbly.Services.Portfolio
{
    public class FilePortfolioService : IFilePortfolioService
    {
        private const long MaxImageSize = 5 * 1024 * 1024;
        private const long MaxVideoSize = 50 * 1024 * 1024;

        private readonly IWebHostEnvironment _environment;

        public FilePortfolioService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<FileUploadResult> UploadPortfolioMediaAsync(IFormFile file, string fileType)
        {
            if (file.Length == 0)
            {
                return FileUploadResult.Failure("Uploaded file is empty.");
            }

            var normalizedType = fileType.Trim();

            var allowedExtensions = normalizedType switch
            {
                "Image" => new[] { ".jpg", ".jpeg", ".png" },
                "Video" => new[] { ".mp4" },
                _ => Array.Empty<string>()
            };

            if (allowedExtensions.Length == 0)
            {
                return FileUploadResult.Failure("Invalid media type.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return FileUploadResult.Failure("Invalid file extension.");
            }

            var maxSize = normalizedType == "Image" ? MaxImageSize : MaxVideoSize;

            if (file.Length > maxSize)
            {
                return FileUploadResult.Failure(
                    normalizedType == "Image"
                        ? "Image size must not exceed 5 MB."
                        : "Video size must not exceed 50 MB.");
            }

            if (!IsValidContentType(file.ContentType, normalizedType))
            {
                return FileUploadResult.Failure("Invalid file content type.");
            }

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadFolder = Path.Combine(webRoot, "uploads", "portfolio");

            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadFolder, fileName);

            await using var stream = new FileStream(physicalPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return FileUploadResult.Success($"/uploads/portfolio/{fileName}");
        }

        public void DeleteFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var cleanPath = relativePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var physicalPath = Path.Combine(webRoot, cleanPath);

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }

        private static bool IsValidContentType(string contentType, string fileType)
        {
            return fileType switch
            {
                "Image" => contentType is "image/jpeg" or "image/png",
                "Video" => contentType == "video/mp4",
                _ => false
            };
        }
    }
}
