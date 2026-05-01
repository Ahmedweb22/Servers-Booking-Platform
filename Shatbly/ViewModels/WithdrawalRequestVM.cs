namespace Shatbly.ViewModels
{
    public class WithdrawalRequestVM
    {
        [Required]
        [Range(1, 1000000, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }
        public decimal AvailableBalance { get; set; }

    }
}
