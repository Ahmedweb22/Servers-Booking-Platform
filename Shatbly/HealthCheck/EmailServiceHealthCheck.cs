using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shatbly.Services.Notification;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shatbly.HealthCheck
{
    public class EmailServiceHealthCheck : IHealthCheck
    {
        private readonly SmtpOptions _options;

        public EmailServiceHealthCheck(IOptions<SmtpOptions> options)
        {
            _options = options.Value;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Host))
            {
                return HealthCheckResult.Degraded("SMTP Host is not configured.");
            }

            try
            {
                // Verify TCP connection to SMTP server
                using var tcpClient = new TcpClient();
                // Set short timeout so health check doesn't hang
                var connectTask = tcpClient.ConnectAsync(_options.Host, _options.Port, cancellationToken);
                var delayTask = Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

                var completedTask = await Task.WhenAny(connectTask.AsTask(), delayTask);
                if (completedTask == delayTask)
                {
                    return HealthCheckResult.Unhealthy($"SMTP server connection timed out on {_options.Host}:{_options.Port}.");
                }

                await connectTask; // Propagate exceptions if connection failed

                if (tcpClient.Connected)
                {
                    return HealthCheckResult.Healthy("SMTP Server is reachable.");
                }

                return HealthCheckResult.Unhealthy($"Failed to connect to SMTP server on {_options.Host}:{_options.Port}.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"SMTP connection check failed: {ex.Message}");
            }
        }
    }
}
