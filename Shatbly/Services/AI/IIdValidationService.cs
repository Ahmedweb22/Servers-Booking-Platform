using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Shatbly.Services.AI
{
    public interface IIdValidationService
    {
        Task<(bool IsValid, string Reason)> ValidateIdCardAsync(IFormFile idCardFile);
    }
}
