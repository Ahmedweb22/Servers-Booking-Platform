using System.Security.Claims;

namespace Shtbly.Services.CurrentWorkerService1
{
    public interface ICurrentWorkerService
    {
        Task<int?> GetCurrentWorkerIdAsync(ClaimsPrincipal user);
    }
}
