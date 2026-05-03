namespace Shatbly.Services.AvailabilityService
{
    public interface IAvailabilityService
    {
        Task<AvailabilityOperationResult> AddAvailabilityAsync(CreateAvailabilityVM model);

        Task<AvailabilityOperationResult> UpdateAvailabilityAsync(int id, CreateAvailabilityVM model);

        Task<AvailabilityOperationResult> DeleteAvailabilityAsync(int id);

        Task<IReadOnlyList<AvailabilityVM>> GetWorkerScheduleAsync(int workerId);
    }
}
