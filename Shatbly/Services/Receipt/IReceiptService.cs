using Shtbly.Models;
using System.Threading.Tasks;

namespace Shtbly.Services.Receipt
{
    public interface IReceiptService
    {
        Task<string> GenerateReceiptPdfAsync(Booking booking);
    }
}
