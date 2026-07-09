namespace Shatbly.ViewModels
{
    public class WorkerVM
    {
        public int Id { get; set; }
        [Required]
        [Display(Name = "First Name")]
        public string FName { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Last Name")]
        public string LName { get; set; } = string.Empty;
        [EmailAddress]
        [Required]
        public string Email { get; set; } = string.Empty;
        [Phone]
        [Required]
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        public string? District { get; set; }

        [Required]
        public IFormFile cv { get; set; }

        [Required]
        [System.ComponentModel.DataAnnotations.Display(Name = "ID Card Photo")]
        public IFormFile IdCardPhoto { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public double? Lat { get; set; }

        public double? Lng { get; set; }
    }
}
