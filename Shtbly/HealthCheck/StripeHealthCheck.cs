using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shtbly.HealthCheck
{
    public class StripeHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _config;

        public StripeHealthCheck(IConfiguration config)
        {
            _config = config;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var secretKey = _config.GetSection("Stripe")["SecretKey"];
                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    return HealthCheckResult.Unhealthy("Stripe SecretKey is not configured.");
                }

                // Initialize balance service and check connectivity
                var balanceService = new BalanceService();
                
                // Set the ApiKey explicitly on request options to make sure it's valid
                var requestOptions = new RequestOptions
                {
                    ApiKey = secretKey
                };

                var balance = await balanceService.GetAsync(requestOptions, cancellationToken);
                if (balance != null)
                {
                    return HealthCheckResult.Healthy("Stripe integration is healthy and connected.");
                }

                return HealthCheckResult.Unhealthy("Stripe integration returned empty response.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Stripe integration check failed: {ex.Message}");
            }
        }
    }
}
