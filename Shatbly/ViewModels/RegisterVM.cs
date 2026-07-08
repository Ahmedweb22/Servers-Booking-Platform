namespace Shatbly.ViewModels
{
    public class RegisterVM
    {
        public int Id { get; set; }
        //[Required]
        [Display(Name = "First Name")]
        public string FName { get; set; } 
        //[Required]
        [Display(Name = "Last Name")]
        public string LName { get; set; } 
        [EmailAddress]
        [Required]
        public string Email { get; set; } = string.Empty;
        [Phone]
        [Required]
        public string Phone { get; set; } = string.Empty;
        [Required]
        public string UserName { get; set; } = string.Empty;
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        public string? District { get; set; }

        [Required]
        public string Street { get; set; } = string.Empty;

        public double? Lat { get; set; }

        public double? Lng { get; set; }
    }
}
