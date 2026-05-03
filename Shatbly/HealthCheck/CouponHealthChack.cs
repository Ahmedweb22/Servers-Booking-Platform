using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Shatbly.HealthCheck
{
    public class CouponHealthChack : IHealthCheck
    {
        private readonly IRepository<Coupon> _repo; 
        public CouponHealthChack(IRepository<Coupon> repo)
        {
            _repo = repo;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            var data = await _repo.GetAsync();

            return data != null
                ? HealthCheckResult.Healthy("Coupon OK")
                : HealthCheckResult.Unhealthy("Coupon Failed");
        }
    }
}
