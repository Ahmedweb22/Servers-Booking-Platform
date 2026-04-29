namespace Shatbly.ViewModels
{
    public class PromotionCodeUpdateResponseVM
    {
        public PromotionCode PromotionCode { get; set; } = new();
        public IEnumerable<Promotion> Promotions { get; set; } = new List<Promotion>();
    }
}
