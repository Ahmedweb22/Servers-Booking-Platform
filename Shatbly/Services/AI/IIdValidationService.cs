using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Shtbly.Services.AI
{
    public interface IIdValidationService
    {
        Task<(bool IsValid, string Reason)> ValidateIdCardAsync(IFormFile idCardFile);
    }
}
