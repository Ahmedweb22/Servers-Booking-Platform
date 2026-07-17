using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shatbly.HealthCheck
{
    public class DatabaseQueriesHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _context;

        public DatabaseQueriesHealthCheck(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var failedQueries = new List<string>();

                // Check Users query
                try { _ = await _context.Users.Take(1).ToListAsync(cancellationToken); }
                catch (Exception ex) { failedQueries.Add($"Users: {ex.Message}"); }

                // Check Bookings query
                try { _ = await _context.Bookings.Take(1).ToListAsync(cancellationToken); }
                catch (Exception ex) { failedQueries.Add($"Bookings: {ex.Message}"); }

                // Check Coupons query
                try { _ = await _context.Coupons.Take(1).ToListAsync(cancellationToken); }
                catch (Exception ex) { failedQueries.Add($"Coupons: {ex.Message}"); }

                // Check WorkerProfiles query
                try { _ = await _context.WorkerProfiles.Take(1).ToListAsync(cancellationToken); }
                catch (Exception ex) { failedQueries.Add($"WorkerProfiles: {ex.Message}"); }

                // Check Payments query
                try { _ = await _context.Payments.Take(1).ToListAsync(cancellationToken); }
                catch (Exception ex) { failedQueries.Add($"Payments: {ex.Message}"); }

                // Check Reviews query
                try { _ = await _context.Reviews.Take(1).ToListAsync(cancellationToken); }
                catch (Exception ex) { failedQueries.Add($"Reviews: {ex.Message}"); }

                // Check Disputes query
                try { _ = await _context.Disputes.Take(1).ToListAsync(cancellationToken); }
                catch (Exception ex) { failedQueries.Add($"Disputes: {ex.Message}"); }

                // Check SupportTickets query
                try { _ = await _context.SupportTickets.Take(1).ToListAsync(cancellationToken); }
                catch (Exception ex) { failedQueries.Add($"SupportTickets: {ex.Message}"); }

                // Check Orders query
                try { _ = await _context.Orders.Take(1).ToListAsync(cancellationToken); }
                catch (Exception ex) { failedQueries.Add($"Orders: {ex.Message}"); }

                // Check Banners query
                try { _ = await _context.Banners.Take(1).ToListAsync(cancellationToken); }
                catch (Exception ex) { failedQueries.Add($"Banners: {ex.Message}"); }

                // Check ServiceCategories query
                try { _ = await _context.ServiceCategories.Take(1).ToListAsync(cancellationToken); }
                catch (Exception ex) { failedQueries.Add($"ServiceCategories: {ex.Message}"); }

                // Check Notifications query
                try { _ = await _context.Notifications.Take(1).ToListAsync(cancellationToken); }
                catch (Exception ex) { failedQueries.Add($"Notifications: {ex.Message}"); }

                if (failedQueries.Any())
                {
                    return HealthCheckResult.Unhealthy($"Database queries failed for: {string.Join(", ", failedQueries)}");
                }

                return HealthCheckResult.Healthy("All database queries are working fine.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Database queries check failed critically: {ex.Message}");
            }
        }
    }
}
