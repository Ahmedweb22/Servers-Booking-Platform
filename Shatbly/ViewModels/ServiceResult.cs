namespace Shatbly.ViewModels
{
    public class ServiceResult
    {
        public bool Succeeded { get; init; }
        public string? ErrorMessage { get; init; }

        public static ServiceResult Success() => new() { Succeeded = true };

        public static ServiceResult Failure(string message) => new()
        {
            Succeeded = false,
            ErrorMessage = message
        };
    }
}
