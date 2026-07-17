using Microsoft.Extensions.Diagnostics.HealthChecks;
using Hangfire;
using Hangfire.Storage;

namespace Shtbly.HealthCheck
{
    public class HangfireHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var storage = JobStorage.Current;
                if (storage == null)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire JobStorage is not initialized."));
                }

                // Verify connectivity by opening a connection to Hangfire storage
                using (var connection = storage.GetConnection())
                {
                    if (connection == null)
                    {
                        return Task.FromResult(HealthCheckResult.Unhealthy("Failed to open a connection to Hangfire storage."));
                    }
                }

                // Verify active servers count
                var monitoringApi = storage.GetMonitoringApi();
                var servers = monitoringApi.Servers();

                if (servers == null || servers.Count == 0)
                {
                    return Task.FromResult(HealthCheckResult.Degraded("Hangfire connection is active, but no active processing servers were found."));
                }

                return Task.FromResult(HealthCheckResult.Healthy($"Hangfire is online. Active servers: {servers.Count}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Hangfire health check failed: {ex.Message}", ex));
            }
        }
    }
}
