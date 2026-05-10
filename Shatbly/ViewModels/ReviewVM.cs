namespace Shatbly.ViewModels
{
    public class ReviewVM
    {
        public int OrderId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string RevieweeId { get; set; }
        public IFormFile? BeforeImage { get; set; }
        public IFormFile? AfterImage { get; set; }

    }
}
