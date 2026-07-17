using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Shtbly.DataAccess;
using Shtbly.UnitOfWork;
using Shtbly.Repositories.IRepositories;
using Shtbly.Services.BookingSystem;
using Shtbly.Services.Chat;
using Shtbly.Services.File_Service;
using Shtbly.Services.Notification;
using Shtbly.Models;

namespace Shtbly.HealthCheck
{
    public class DependencyInjectionHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;

        public DependencyInjectionHealthCheck(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var missingServices = new List<string>();

            void CheckService<T>() where T : class
            {
                // We resolve from a scoped provider if requested, but GetService from root is safe for transient/scoped if needed.
                // Using CreateScope is cleaner for resolving scoped dependencies.
                using (var scope = _serviceProvider.CreateScope())
                {
                    var service = scope.ServiceProvider.GetService<T>();
                    if (service == null)
                    {
                        missingServices.Add(typeof(T).Name);
                    }
                }
            }

            CheckService<ApplicationDbContext>();
            CheckService<IUnitOfWork>();
            CheckService<IRepository<Booking>>();
            CheckService<IRepository<Coupon>>();
            CheckService<IRepository<User>>();
            CheckService<IRepository<WorkerProfile>>();
            CheckService<IRepository<Order>>();
            CheckService<IRepository<ChatMessage>>();
            CheckService<IBookingSystemService>();
            CheckService<IChatService>();
            CheckService<IFileService>();
            CheckService<INotificationService>();
            CheckService<IEmailService>();

            if (missingServices.Any())
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Dependency Injection Verification Failed. The following registered services failed to resolve: {string.Join(", ", missingServices)}"
                ));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "All critical application services, features, and repositories resolved successfully from the dependency injection container."
            ));
        }
    }
}
