namespace Shatbly.ViewModels
{
    public class EarningsDashboardVM
    {
        public decimal TotalEarnings { get; set; }
        public decimal MonthlyEarnings { get; set; }
        public decimal PendingBalance { get; set; }
        public List<EarningTransactionVM> RecentTransactions { get; set; } = [];
    }
    public class EarningTransactionVM
    {
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
