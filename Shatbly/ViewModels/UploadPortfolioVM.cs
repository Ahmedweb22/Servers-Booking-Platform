namespace Shatbly.ViewModels
{
    public class UploadPortfolioVM
    {
        [Required]
        [Display(Name = "Media File")]
        public IFormFile? File { get; set; }

        [Required]
        [Display(Name = "File Type")]
        public string FileType { get; set; } = "Image";
        [MaxLength(500)]
        public string? Caption { get; set; }
    }
}
