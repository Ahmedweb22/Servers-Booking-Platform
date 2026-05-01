namespace Shatbly.ViewModels
{
    public class PortfolioOperationResult
    {
        public bool Succeeded { get; init; }
        public string? ErrorMessage { get; init; }

        public static PortfolioOperationResult Success()
        {
            return new PortfolioOperationResult { Succeeded = true };
        }

        public static PortfolioOperationResult Failure(string message)
        {
            return new PortfolioOperationResult
            {
                Succeeded = false,
                ErrorMessage = message
            };
        }
    }
}
