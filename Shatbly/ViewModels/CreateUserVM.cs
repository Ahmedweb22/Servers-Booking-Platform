namespace Shatbly.ViewModels
{
    public class CreateUserVM
    {
        public string Id { get; set; }
        [Required]
        [Display(Name = "First Name")]
        [RegularExpression(@"^[a-zA-Z\u0600-\u06FF]+$", ErrorMessage = "First Name must contain only letters.")]
        public string FName { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Last Name")]
        [RegularExpression(@"^[a-zA-Z\u0600-\u06FF]+$", ErrorMessage = "Last Name must contain only letters.")]
        public string LName { get; set; } = string.Empty;
        [Required]
        public string UserName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [Phone]
        public string Phone { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required]
        public string RoleName { get; set; }
        // public IEnumerable<IdentityRole> Roles { get; set; }
         public List<IdentityRole> Roles { get; set; }

    }
}
