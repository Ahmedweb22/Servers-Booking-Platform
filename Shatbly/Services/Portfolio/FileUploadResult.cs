namespace Shatbly.Services.Portfolio
{
    public class FileUploadResult
    {
        public bool Succeeded { get; init; }
        public string? FilePath { get; init; }
        public string? ErrorMessage { get; init; }

        public static FileUploadResult Success(string filePath)
        {
            return new FileUploadResult
            {
                Succeeded = true,
                FilePath = filePath
            };
        }

        public static FileUploadResult Failure(string message)
        {
            return new FileUploadResult
            {
                Succeeded = false,
                ErrorMessage = message
            };
        }
    }
}
