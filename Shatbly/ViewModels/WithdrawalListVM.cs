namespace Shatbly.ViewModels
{
    public class WithdrawalListVM
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
