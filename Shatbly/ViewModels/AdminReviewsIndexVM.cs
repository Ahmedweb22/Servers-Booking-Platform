using Shtbly.Models;

namespace Shtbly.ViewModels
{
    public class AdminReviewsIndexVM
    {
        public IEnumerable<Review>? Reviews { get; set; }
        public string? Search { get; set; }
        public int CurrentPage { get; set; }
        public double TotalPages { get; set; }
    }
}
