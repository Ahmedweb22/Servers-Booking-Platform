namespace Shatbly.Services.File_Service
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<FileUploadResult> UploadFileAsync(
            IFormFile file,
            string folderPath,
            long maxSizeInBytes,
            string[] allowedExtensions)
        {
            if (file.Length == 0)
            {
                return FileUploadResult.Failure("The uploaded file is empty.");
            }

            if (file.Length > maxSizeInBytes)
            {
                return FileUploadResult.Failure($"The file size must not exceed {maxSizeInBytes / 1024 / 1024} MB.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return FileUploadResult.Failure($"Only the following file types are allowed: {string.Join(", ", allowedExtensions)}");
            }
            if (extension == ".pdf")
            {
                await using var validationStream = file.OpenReadStream();
                var header = new byte[4];
                var bytesRead = await validationStream.ReadAsync(header);

                if (bytesRead < 4 || header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46)
                {
                    return FileUploadResult.Failure("Invalid PDF file.");
                }
            }
            else if (extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp")
            {
                await using var validationStream = file.OpenReadStream();
                var header = new byte[8];
                var bytesRead = await validationStream.ReadAsync(header);

                bool isValidImage = false;
                if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                {
                    isValidImage = true;
                }
                else if (bytesRead >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                {
                    isValidImage = true;
                }
                else if (bytesRead >= 4 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
                {
                    isValidImage = true;
                }
                else if (bytesRead >= 4 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46)
                {
                    isValidImage = true;
                }

                if (!isValidImage)
                {
                    return FileUploadResult.Failure("Invalid image file format signature.");
                }
            }
            var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadDirectory = Path.Combine(webRootPath, folderPath);

            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            var safeFileName = $"{Guid.NewGuid()}{extension}";
            var physicalPath = Path.Combine(uploadDirectory, safeFileName);

            await using var fileStream = new FileStream(physicalPath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            var relativePath = Path.Combine(folderPath, safeFileName).Replace("\\", "/");

            return FileUploadResult.Success(relativePath);
        }
    }
}
