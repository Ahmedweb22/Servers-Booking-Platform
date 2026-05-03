namespace Shatbly.ViewModels
{
    public class EditWorkerProfileVM
    {
        public int Id { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Bio { get; set; } = string.Empty;

        [Display(Name = "Available")]
        public bool IsAvailable { get; set; }

        [Display(Name = "Accepts Online Bookings")]
        public bool AcceptsOnline { get; set; }

        public string? ExistingCVPath { get; set; }

        [Display(Name = "Upload CV")]
        public IFormFile? CVFile { get; set; }
    }
}
