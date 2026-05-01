using System.Security.Claims;

namespace Shatbly.Services.CurrentWorkerService1
{
    public interface ICurrentWorkerService
    {
        Task<int?> GetCurrentWorkerIdAsync(ClaimsPrincipal user);
    }
}
