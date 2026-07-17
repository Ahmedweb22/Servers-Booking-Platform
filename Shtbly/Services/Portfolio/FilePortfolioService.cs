using Shtbly.Services.Portfolio;
namespace Shtbly.Services.Portfolio
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

            if (!await HasValidSignatureAsync(file, extension, normalizedType))
            {
                return FileUploadResult.Failure("Invalid file signature.");
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
            if (cleanPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Contains(".."))
            {
                return;
            }

            var webRootFullPath = Path.GetFullPath(webRoot);
            var physicalPath = Path.GetFullPath(Path.Combine(webRootFullPath, cleanPath));
            var webRootPrefix = webRootFullPath.EndsWith(Path.DirectorySeparatorChar)
                ? webRootFullPath
                : webRootFullPath + Path.DirectorySeparatorChar;

            if (!physicalPath.StartsWith(webRootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

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

        private static async Task<bool> HasValidSignatureAsync(IFormFile file, string extension, string fileType)
        {
            await using var validationStream = file.OpenReadStream();
            var header = new byte[12];
            var bytesRead = await validationStream.ReadAsync(header);

            if (fileType == "Image")
            {
                return extension switch
                {
                    ".jpg" or ".jpeg" => bytesRead >= 3 &&
                                         header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                    ".png" => bytesRead >= 4 &&
                              header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                    _ => false
                };
            }

            return fileType == "Video" &&
                   extension == ".mp4" &&
                   bytesRead >= 12 &&
                   header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70;
        }
    }
}
