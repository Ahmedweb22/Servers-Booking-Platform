using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shatbly.DataAccess;
using Shatbly.Models;
using Shatbly.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shatbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN} , {SD.ROLE_SUPER_ADMIN}")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var usersCount = await _context.Users.CountAsync();
            var serviceCategoryCount = await _context.ServiceCategories.CountAsync(x => x.IsActive);
            var orderCount = await _context.Orders.CountAsync();

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatuses.Completed || o.Status == OrderStatuses.Confirmed)
                .SumAsync(o => o.TotalPrice);

            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Service)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            // 1. Orders Over Time (Monthly counts for last 6 months)
            var monthlyOrderLabels = new List<string>();
            var monthlyOrderCounts = new List<int>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.Today.AddMonths(-i);
                var label = monthDate.ToString("MMM");
                monthlyOrderLabels.Add(label);

                var startOfMonth = new DateTime(monthDate.Year, monthDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1);

                var count = await _context.Orders
                    .CountAsync(o => o.CreatedAt >= startOfMonth && o.CreatedAt < endOfMonth);
                monthlyOrderCounts.Add(count);
            }

            // 2. User Distribution (Admins, Workers, Customers)
            var roles = await _context.Roles.ToListAsync();
            var userRoles = await _context.UserRoles.ToListAsync();
            var roleCounts = userRoles
                .GroupBy(ur => ur.RoleId)
                .Select(g => new { RoleId = g.Key, Count = g.Count() })
                .ToList();
            var roleNames = roles.ToDictionary(r => r.Id, r => r.Name ?? "Unknown");

            var userDistributionLabels = new List<string>();
            var userDistributionCounts = new List<int>();
            foreach (var rc in roleCounts)
            {
                var roleName = roleNames.GetValueOrDefault(rc.RoleId, "Other");
                if (roleName.Equals(SD.ROLE_CUSTOMER, StringComparison.OrdinalIgnoreCase)) roleName = "Customers";
                else if (roleName.Equals(SD.ROLE_WORKER, StringComparison.OrdinalIgnoreCase)) roleName = "Workers";
                else if (roleName.Equals(SD.ROLE_ADMIN, StringComparison.OrdinalIgnoreCase)) roleName = "Admins";
                else if (roleName.Equals(SD.ROLE_SUPER_ADMIN, StringComparison.OrdinalIgnoreCase)) roleName = "Super Admins";

                userDistributionLabels.Add(roleName);
                userDistributionCounts.Add(rc.Count);
            }

            // If no user distribution data exists, supply default fallback values to prevent empty pie chart
            if (!userDistributionCounts.Any())
            {
                userDistributionLabels.AddRange(new[] { "Customers", "Workers", "Admins" });
                userDistributionCounts.AddRange(new[] { 0, 0, 0 });
            }

            // 3. Services Usage (Most requested services)
            var servicesUsage = await _context.Orders
                .Where(o => o.Service != null)
                .GroupBy(o => o.Service!.NameEn)
                .Select(g => new { ServiceName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(6)
                .ToListAsync();

            var servicesUsageLabels = servicesUsage.Select(su => su.ServiceName).ToList();
            var servicesUsageCounts = servicesUsage.Select(su => su.Count).ToList();

            var viewModel = new DashboardStatsCardCountVM
            {
                ServicesCategoriesCount = serviceCategoryCount,
                OrdersCount = orderCount,
                UsersCount = usersCount,
                TotalRevenue = totalRevenue,
                RecentOrders = recentOrders,
                MonthlyOrderLabels = monthlyOrderLabels,
                MonthlyOrderCounts = monthlyOrderCounts,
                UserDistributionLabels = userDistributionLabels,
                UserDistributionCounts = userDistributionCounts,
                ServicesUsageLabels = servicesUsageLabels,
                ServicesUsageCounts = servicesUsageCounts
            };

            return View(viewModel);
        }
    }
}
