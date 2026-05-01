namespace Shatbly.Services.WorkerProfileService
{
    public class WorkerProfileService : IWorkerProfileService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WorkerProfileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<WorkerProfile?> GetByUserIdAsync(string userId)
        {
            return await _unitOfWork.WorkerProfiles.GetOneAsync(
                w => w.UserId == userId,
                tracking: false);
        }
    }
}
