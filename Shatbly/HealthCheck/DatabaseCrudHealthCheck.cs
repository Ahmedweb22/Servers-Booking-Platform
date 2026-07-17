using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Shtbly.DataAccess;
using Shtbly.Models;

namespace Shtbly.HealthCheck
{
    public class DatabaseCrudHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _context;

        public DatabaseCrudHealthCheck(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Verify we can connect to the database
                if (!await _context.Database.CanConnectAsync(cancellationToken))
                {
                    return HealthCheckResult.Unhealthy("Cannot connect to SQL Server database.");
                }

                // Verify CRUD operations using transaction rollback to avoid polluting the DB
                using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
                {
                    string uniqueCode = "HEALTH_TEMP_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                    // 1. CREATE
                    var testCoupon = new Coupon
                    {
                        Code = uniqueCode,
                        DiscountValue = 10.00m,
                        DiscountType = DiscountType.FixedAmount,
                        MaxUses = 1,
                        UsedCount = 0,
                        ValidFrom = DateTime.UtcNow,
                        ValidUntil = DateTime.UtcNow.AddDays(1),
                        IsActive = false
                    };

                    _context.Coupons.Add(testCoupon);
                    await _context.SaveChangesAsync(cancellationToken);

                    int generatedId = testCoupon.Id;
                    if (generatedId <= 0)
                    {
                        return HealthCheckResult.Unhealthy("CRUD Check Failed: Insert operation did not generate a valid ID.");
                    }

                    // Detach test entity to force reload from DB
                    _context.Entry(testCoupon).State = EntityState.Detached;

                    // 2. READ
                    var retrieved = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == generatedId, cancellationToken);
                    if (retrieved == null || retrieved.Code != uniqueCode)
                    {
                        return HealthCheckResult.Unhealthy("CRUD Check Failed: Select operation failed to retrieve the inserted record.");
                    }

                    // 3. UPDATE
                    retrieved.MaxUses = 5;
                    _context.Coupons.Update(retrieved);
                    await _context.SaveChangesAsync(cancellationToken);

                    // Detach again
                    _context.Entry(retrieved).State = EntityState.Detached;

                    var updated = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == generatedId, cancellationToken);
                    if (updated == null || updated.MaxUses != 5)
                    {
                        return HealthCheckResult.Unhealthy("CRUD Check Failed: Update operation failed to apply changes.");
                    }

                    // 4. DELETE
                    _context.Coupons.Remove(updated);
                    await _context.SaveChangesAsync(cancellationToken);

                    var deleted = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == generatedId, cancellationToken);
                    if (deleted != null)
                    {
                        return HealthCheckResult.Unhealthy("CRUD Check Failed: Delete operation failed to remove the record.");
                    }

                    // Rollback the transaction to be absolutely clean
                    await transaction.RollbackAsync(cancellationToken);
                }

                return HealthCheckResult.Healthy("SQL Server Database is online and CRUD operations are fully functional.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Database CRUD Check encountered an error: {ex.Message}", ex);
            }
        }
    }
}
