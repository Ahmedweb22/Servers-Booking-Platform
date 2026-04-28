
namespace Shatbly.Services.BookingSystem;

public class BookingCreateResult
{
    public bool Succeeded { get; init; }
    public int? BookingId { get; init; }
    public BookingWizardViewModel ViewModel { get; init; } = new();
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ValidationErrors { get; init; }
        = new Dictionary<string, IReadOnlyList<string>>();
    public string? SuccessMessage { get; init; }
}
