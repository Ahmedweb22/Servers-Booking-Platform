using System.Security.Claims;

namespace Shatbly.Services.CurrentWorkerService1
{

    public class CurrentWorkerService : ICurrentWorkerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CurrentWorkerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<int?> GetCurrentWorkerIdAsync(ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var profile = await _unitOfWork.WorkerProfiles
                .GetOneAsync(x => x.UserId == userId, tracking: false);

            return profile?.Id;
        }
    }
}
