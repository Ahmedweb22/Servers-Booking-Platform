namespace Shatbly.ViewModels
{
    public class AvailabilityOperationResult
    {
        public bool Succeeded { get; init; }

        public string? ErrorMessage { get; init; }

        public static AvailabilityOperationResult Success()
        {
            return new AvailabilityOperationResult { Succeeded = true };
        }

        public static AvailabilityOperationResult Failure(string message)
        {
            return new AvailabilityOperationResult
            {
                Succeeded = false,
                ErrorMessage = message
            };
        }
    }
}
