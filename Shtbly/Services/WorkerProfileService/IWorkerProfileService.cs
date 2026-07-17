namespace Shtbly.Services.WorkerProfileService
{
    public interface IWorkerProfileService
    {
        Task<WorkerProfile?> GetByUserIdAsync(string userId);
    }
}