using System.Collections.Generic;

namespace Shtbly.ViewModels
{
    public class ServiceReportItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageHourlyRate { get; set; }
    }

    public class AdminReportVM
    {
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int PendingBookings { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal AverageBookingValue { get; set; }

        public int TotalCustomers { get; set; }
        public int TotalWorkers { get; set; }
        public int TotalAdmins { get; set; }

        public List<ServiceReportItem> ServicePerformance { get; set; } = new();
    }
}
