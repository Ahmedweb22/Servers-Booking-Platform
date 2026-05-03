    using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shatbly.Utilities.Dbintializes;
namespace Shatbly.HealthCheck
{

    public class WorkerHealthCheck : IHealthCheck
    {
        private readonly IRepository<Booking> _repo;

        public WorkerHealthCheck(IRepository<Booking> repo)
        {
            _repo = repo;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = await _repo.GetAsync();

            return data != null
                ? HealthCheckResult.Healthy("Booking OK")
                : HealthCheckResult.Unhealthy("Booking Failed");
        }
    }
}
