
namespace Shatbly.Services.AvailabilityService
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AvailabilityService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AvailabilityOperationResult> AddAvailabilityAsync(CreateAvailabilityVM model)
        {
            if (await HasOverlapAsync(model.WorkerId, model.DayOfWeek, model.StartTime, model.EndTime))
            {
                return AvailabilityOperationResult.Failure("This time slot overlaps with an existing availability.");
            }

            var availability = new Avalability
            {
                WorkerId = model.WorkerId,
                DayOfWeek = (Models.DayOfWeek)model.DayOfWeek,
                StartTime = model.StartTime,
                EndTime = model.EndTime
            };

            await _unitOfWork.Availabilities.CreateAsync(availability);
            await _unitOfWork.CommitAsync();

            return AvailabilityOperationResult.Success();
        }

        public async Task<AvailabilityOperationResult> UpdateAvailabilityAsync(int id, CreateAvailabilityVM model)
        {
            var availability = await _unitOfWork.Availabilities.GetOneAsync(a => a.Id == id);

            if (availability is null)
            {
                return AvailabilityOperationResult.Failure("Availability slot was not found.");
            }

            if (await HasOverlapAsync(model.WorkerId, model.DayOfWeek, model.StartTime, model.EndTime, id))
            {
                return AvailabilityOperationResult.Failure("This time slot overlaps with an existing availability.");
            }

            availability.DayOfWeek = (Models.DayOfWeek)model.DayOfWeek;
            availability.StartTime = model.StartTime;
            availability.EndTime = model.EndTime;

            _unitOfWork.Availabilities.Update(availability);
            await _unitOfWork.CommitAsync();

            return AvailabilityOperationResult.Success();
        }

        public async Task<AvailabilityOperationResult> DeleteAvailabilityAsync(int id)
        {
            var availability = await _unitOfWork.Availabilities.GetOneAsync(a => a.Id == id);

            if (availability is null)
            {
                return AvailabilityOperationResult.Failure("Availability slot was not found.");
            }

            _unitOfWork.Availabilities.Delete(availability);
            await _unitOfWork.CommitAsync();

            return AvailabilityOperationResult.Success();
        }

        public async Task<IReadOnlyList<AvailabilityVM>> GetWorkerScheduleAsync(int workerId)
        {
            var availability = await _unitOfWork.Availabilities.GetAsync(
                a => a.WorkerId == workerId,
                tracking: false);

            return availability
                .OrderBy(a => a.DayOfWeek)
                .ThenBy(a => a.StartTime)
                .Select(a => new AvailabilityVM
                {
                    Id = a.Id,
                    WorkerId = a.WorkerId,
                    DayOfWeek = (DayOfWeek)a.DayOfWeek,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime
                })
                .ToList();
        }

        private async Task<bool> HasOverlapAsync(
            int workerId,
            DayOfWeek dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            int? ignoredAvailabilityId = null)
        {
            var existingSlots = await _unitOfWork.Availabilities.GetAsync(
                a => a.WorkerId == workerId &&
                     a.DayOfWeek == dayOfWeek &&
                     (!ignoredAvailabilityId.HasValue || a.Id != ignoredAvailabilityId.Value),
                tracking: false);

            return existingSlots.Any(a =>
                startTime < a.EndTime &&
                a.StartTime < endTime);
        }
    }
}