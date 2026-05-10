namespace Shatbly.Services.File_Service
{
    public interface IFileService
    {
        Task<FileUploadResult> UploadFileAsync(
        IFormFile file,
        string folderPath,
        long maxSizeInBytes,
        string[] allowedExtensions);
    }
}
