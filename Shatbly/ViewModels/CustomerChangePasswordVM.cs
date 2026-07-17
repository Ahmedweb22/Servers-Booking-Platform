using System.ComponentModel.DataAnnotations;

namespace Shtbly.ViewModels
{
    public class CustomerChangePasswordVM
    {
        [Required(ErrorMessage = "CurrentPasswordRequired")]
        [DataType(DataType.Password)]
        [Display(Name = "CurrentPassword")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "NewPasswordRequired")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "NewPasswordLength")]
        [DataType(DataType.Password)]
        [Display(Name = "NewPassword")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "ConfirmPasswordRequired")]
        [DataType(DataType.Password)]
        [Display(Name = "ConfirmPassword")]
        [Compare(nameof(NewPassword), ErrorMessage = "PasswordsDoNotMatch")]
        public string ConfirmPassword { get; set; }
    }
}
