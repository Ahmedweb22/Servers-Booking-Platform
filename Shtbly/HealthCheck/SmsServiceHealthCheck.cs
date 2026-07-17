using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shtbly.Services.Notification;
using System.Threading;
using System.Threading.Tasks;

namespace Shtbly.HealthCheck
{
    public class SmsServiceHealthCheck : IHealthCheck
    {
        private readonly ISmsService _smsService;

        public SmsServiceHealthCheck(ISmsService smsService)
        {
            _smsService = smsService;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_smsService != null)
            {
                return Task.FromResult(HealthCheckResult.Healthy($"SMS Service is active: {_smsService.GetType().Name}"));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy("SMS Service is not registered."));
        }
    }
}
