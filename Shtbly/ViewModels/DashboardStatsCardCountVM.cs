using Shtbly.Models;
using System.Collections.Generic;

namespace Shtbly.ViewModels
{
    public class DashboardStatsCardCountVM
    {
        public int ServicesCategoriesCount { get; set; }
        public int OrdersCount { get; set; }
        public int UsersCount { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<Order> RecentOrders { get; set; } = new();

        public List<string> MonthlyOrderLabels { get; set; } = new();
        public List<int> MonthlyOrderCounts { get; set; } = new();

        public List<string> UserDistributionLabels { get; set; } = new();
        public List<int> UserDistributionCounts { get; set; } = new();

        public List<string> ServicesUsageLabels { get; set; } = new();
        public List<int> ServicesUsageCounts { get; set; } = new();
    }
}
