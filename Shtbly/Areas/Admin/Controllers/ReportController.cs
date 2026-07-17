using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shtbly.DataAccess;
using Shtbly.Models;
using Shtbly.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shtbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN} , {SD.ROLE_SUPER_ADMIN}")]
    public class ReportController : Controller
    {
        private readonly Shtbly.UnitOfWork.IUnitOfWork _unitOfWork;

        public ReportController(Shtbly.UnitOfWork.IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var bookings = await _unitOfWork.Orders.GetAsync(tracking: false);
            var totalBookings = bookings.Count;
            var completedBookings = bookings.Count(b => b.Status == OrderStatuses.Completed);
            var cancelledBookings = bookings.Count(b => b.Status == OrderStatuses.Cancelled);
            var pendingBookings = bookings.Count(b => b.Status == OrderStatuses.Pending);

            var totalRevenue = bookings
                .Where(b => b.Status == OrderStatuses.Completed || b.Status == OrderStatuses.Confirmed)
                .Sum(b => b.TotalPrice);

            var averageBookingValue = totalBookings > 0 ? totalRevenue / totalBookings : 0;

            // Get user role distribution
            var roles = await _unitOfWork.Roles.GetAsync(tracking: false);
            var userRoles = await _unitOfWork.UserRoles.GetAsync(tracking: false);
            var roleCounts = userRoles
                .GroupBy(ur => ur.RoleId)
                .ToDictionary(g => g.Key, g => g.Count());
            var roleNames = roles.ToDictionary(r => r.Id, r => r.Name ?? "Unknown");

            var totalCustomers = 0;
            var totalWorkers = 0;
            var totalAdmins = 0;

            foreach (var rc in roleCounts)
            {
                var roleName = roleNames.GetValueOrDefault(rc.Key, "Other");
                if (roleName.Equals(SD.ROLE_CUSTOMER, StringComparison.OrdinalIgnoreCase)) totalCustomers = rc.Value;
                else if (roleName.Equals(SD.ROLE_WORKER, StringComparison.OrdinalIgnoreCase)) totalWorkers = rc.Value;
                else if (roleName.Equals(SD.ROLE_ADMIN, StringComparison.OrdinalIgnoreCase) || roleName.Equals(SD.ROLE_SUPER_ADMIN, StringComparison.OrdinalIgnoreCase)) totalAdmins += rc.Value;
            }

            // Services Performance
            var allOrders = await _unitOfWork.Orders.GetAsync(
                expression: o => o.Service != null,
                includes: new System.Linq.Expressions.Expression<System.Func<Order, object>>[] { o => o.Service },
                tracking: false);

            var servicesUsage = allOrders
                .GroupBy(o => o.Service!.Id)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    CategoryName = g.First().Service!.NameEn,
                    BookingCount = g.Count(),
                    TotalRevenue = g.Where(o => o.Status == OrderStatuses.Completed || o.Status == OrderStatuses.Confirmed).Sum(o => o.TotalPrice)
                })
                .ToList();

            // Get average hourly rates for services in category
            var workerServices = await _unitOfWork.WorkerServices.GetAsync(tracking: false);
            var serviceRates = workerServices
                .GroupBy(ws => ws.CategoryId)
                .Select(g => new { CategoryId = g.Key, AvgRate = g.Average(ws => ws.HourlyRate) })
                .ToDictionary(g => g.CategoryId, g => g.AvgRate);

            var servicePerformanceList = new List<ServiceReportItem>();
            foreach (var su in servicesUsage)
            {
                var avgRate = serviceRates.GetValueOrDefault(su.CategoryId, 0);
                servicePerformanceList.Add(new ServiceReportItem
                {
                    CategoryName = su.CategoryName,
                    BookingCount = su.BookingCount,
                    TotalRevenue = su.TotalRevenue,
                    AverageHourlyRate = avgRate
                });
            }

            var vm = new AdminReportVM
            {
                TotalBookings = totalBookings,
                CompletedBookings = completedBookings,
                CancelledBookings = cancelledBookings,
                PendingBookings = pendingBookings,
                TotalRevenue = totalRevenue,
                AverageBookingValue = averageBookingValue,
                TotalCustomers = totalCustomers,
                TotalWorkers = totalWorkers,
                TotalAdmins = totalAdmins,
                ServicePerformance = servicePerformanceList
            };

            return View(vm);
        }
    }
}
