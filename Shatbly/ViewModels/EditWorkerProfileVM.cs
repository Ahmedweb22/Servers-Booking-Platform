using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Shtbly.ViewModels
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
        public string? ExistingProfilePicturePath { get; set; }
 
        [Display(Name = "Upload CV")]
        public IFormFile? CVFile { get; set; }
        
        [Display(Name = "Upload Profile Photo")]
        public IFormFile? ProfilePictureFile { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Hourly rate must be greater than 0.")]
        [Display(Name = "Hourly Rate")]
        public decimal HourlyRate { get; set; }

        [Required(ErrorMessage = "Please select a service category.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        public IEnumerable<SelectListItem>? Categories { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New Password (optional)")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string? ConfirmPassword { get; set; }
    }
}
