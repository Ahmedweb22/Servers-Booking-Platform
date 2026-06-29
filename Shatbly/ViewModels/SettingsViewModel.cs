namespace Shatbly.ViewModels
{
    public class SettingViewModel
    {
        // ── Profile ──────────────────────────────────────────────────
        public class ProfileViewModel
        {
            [Required(ErrorMessage = "Name is required")]
            [StringLength(100, MinimumLength = 2,
                ErrorMessage = "Name must be between 2 and 100 characters")]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Enter a valid email address")]
            [Display(Name = "Email Address")]
            public string Email { get; set; } = string.Empty;

            // Display-only — prefilled from the DB, not updated here
            public string? CurrentName { get; set; }
            public string? CurrentEmail { get; set; }
        }

        // ── Password ─────────────────────────────────────────────────
        public class PasswordViewModel
        {
            [Required(ErrorMessage = "Current password is required")]
            [DataType(DataType.Password)]
            [Display(Name = "Current Password")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "New password is required")]
            [StringLength(100, MinimumLength = 8,
                ErrorMessage = "Password must be at least 8 characters")]
            [RegularExpression(
                @"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
                ErrorMessage = "Must contain one uppercase letter, one number, and one special character")]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your new password")]
            [DataType(DataType.Password)]
            [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
            [Display(Name = "Confirm Password")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // ── Page wrapper ─────────────────────────────────────────────
        public class SettingsViewModel
        {
            public ProfileViewModel Profile { get; set; } = new();
            public PasswordViewModel Password { get; set; } = new();

            /// "profile" | "password"
            public string ActiveTab { get; set; } = "profile";
        }
    }
}
