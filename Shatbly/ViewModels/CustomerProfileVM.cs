using System.ComponentModel.DataAnnotations;

namespace Shatbly.ViewModels
{
    public class CustomerProfileVM
    {
        [Required(ErrorMessage = "FirstNameRequired")]
        [Display(Name = "FirstName")]
        [StringLength(100, ErrorMessage = "FirstNameMaxLength")]
        public string FName { get; set; }

        [Required(ErrorMessage = "LastNameRequired")]
        [Display(Name = "LastName")]
        [StringLength(100, ErrorMessage = "LastNameMaxLength")]
        public string LName { get; set; }

        [Required(ErrorMessage = "PhoneRequired")]
        [Phone(ErrorMessage = "PhoneInvalid")]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [Display(Name = "Address")]
        [StringLength(255, ErrorMessage = "AddressMaxLength")]
        public string? Address { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}
