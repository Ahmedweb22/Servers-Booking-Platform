using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace Shtbly.HealthCheck
{
    public class DiskSpaceHealthCheck : IHealthCheck
    {
        private readonly IWebHostEnvironment _env;

        public DiskSpaceHealthCheck(IWebHostEnvironment env)
        {
            _env = env;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var driveInfo = new DriveInfo(_env.ContentRootPath);
                long freeSpaceBytes = driveInfo.AvailableFreeSpace;
                long totalSpaceBytes = driveInfo.TotalSize;
                double freePercentage = (double)freeSpaceBytes / totalSpaceBytes * 100;

                // If less than 1GB free or less than 5% free, mark as unhealthy
                if (freeSpaceBytes < 1024L * 1024 * 1024 || freePercentage < 5)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy($"Low disk space. Free: {freeSpaceBytes / 1024 / 1024} MB ({freePercentage:F2}%)"));
                }

                // If less than 5GB free, mark as degraded
                if (freeSpaceBytes < 5L * 1024 * 1024 * 1024)
                {
                    return Task.FromResult(HealthCheckResult.Degraded($"Disk space is getting low. Free: {freeSpaceBytes / 1024 / 1024} MB ({freePercentage:F2}%)"));
                }

                return Task.FromResult(HealthCheckResult.Healthy($"Disk space OK. Free: {freeSpaceBytes / 1024 / 1024} MB ({freePercentage:F2}%)"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Failed to read disk space", ex));
            }
        }
    }

    public class MemoryHealthCheck : IHealthCheck
    {
        // 1.5 GB limit for Unhealthy
        private const long ThresholdBytes = 1536L * 1024 * 1024;
        
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                long memoryUsedBytes = process.WorkingSet64;

                if (memoryUsedBytes > ThresholdBytes)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy($"High memory usage: {memoryUsedBytes / 1024 / 1024} MB (Limit: {ThresholdBytes / 1024 / 1024} MB)"));
                }

                if (memoryUsedBytes > ThresholdBytes * 0.8)
                {
                    return Task.FromResult(HealthCheckResult.Degraded($"Elevated memory usage: {memoryUsedBytes / 1024 / 1024} MB"));
                }

                return Task.FromResult(HealthCheckResult.Healthy($"Memory usage OK: {memoryUsedBytes / 1024 / 1024} MB"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Failed to read memory usage", ex));
            }
        }
    }

    public class FileSystemWriteHealthCheck : IHealthCheck
    {
        private readonly IWebHostEnvironment _env;

        public FileSystemWriteHealthCheck(IWebHostEnvironment env)
        {
            _env = env;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var uploadsPath = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var testFile = Path.Combine(uploadsPath, $"healthcheck_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                return Task.FromResult(HealthCheckResult.Healthy("File system write permission OK."));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Cannot write to uploads directory.", ex));
            }
        }
    }

    public class GoogleAuthConfigHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _config;

        public GoogleAuthConfigHealthCheck(IConfiguration config)
        {
            _config = config;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var clientId = _config["Authentication:Google:ClientId"];
            var clientSecret = _config["Authentication:Google:ClientSecret"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Google OAuth ClientId or ClientSecret is missing. Social login will fail."));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Google Auth configuration OK."));
        }
    }

    public class JwtConfigHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _config;

        public JwtConfigHealthCheck(IConfiguration config)
        {
            _config = config;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var signingKey = _config["Jwt:SigningKey"];

            if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 16)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("JWT SigningKey is missing or too short. API authentication will fail."));
            }

            return Task.FromResult(HealthCheckResult.Healthy("JWT configuration OK."));
        }
    }
}
