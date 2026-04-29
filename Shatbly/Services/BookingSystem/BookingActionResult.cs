namespace Shatbly.Services.BookingSystem;

public class BookingActionResult
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public int BookingId { get; init; }
    public string Message { get; init; } = string.Empty;
}
