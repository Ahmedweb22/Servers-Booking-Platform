namespace Shatbly.Services.File_Service
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<FileUploadResult> UploadPdfAsync(
            IFormFile file,
            string folderPath,
            long maxSizeInBytes)
        {
            if (file.Length == 0)
            {
                return FileUploadResult.Failure("The uploaded file is empty.");
            }

            if (file.Length > maxSizeInBytes)
            {
                return FileUploadResult.Failure("The CV file size must not exceed 5 MB.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension != ".pdf" || file.ContentType != "application/pdf")
            {
                return FileUploadResult.Failure("Only PDF files are allowed.");
            }

            await using var validationStream = file.OpenReadStream();
            var header = new byte[4];
            var bytesRead = await validationStream.ReadAsync(header);

            if (bytesRead < 4 || header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46)
            {
                return FileUploadResult.Failure("Invalid PDF file.");
            }

            var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadDirectory = Path.Combine(webRootPath, folderPath);

            Directory.CreateDirectory(uploadDirectory);

            var safeFileName = $"{Guid.NewGuid():N}.pdf";
            var physicalPath = Path.Combine(uploadDirectory, safeFileName);

            await using var fileStream = new FileStream(physicalPath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            var relativePath = $"/{folderPath.Replace("\\", "/").Trim('/')}/{safeFileName}";

            return FileUploadResult.Success(relativePath);
        }
    }
}
