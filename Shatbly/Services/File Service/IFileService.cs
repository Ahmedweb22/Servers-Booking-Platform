namespace Shatbly.Services.File_Service
{
    public interface IFileService
    {
        Task<FileUploadResult> UploadPdfAsync(
        IFormFile file,
        string folderPath,
        long maxSizeInBytes);
    }
}
