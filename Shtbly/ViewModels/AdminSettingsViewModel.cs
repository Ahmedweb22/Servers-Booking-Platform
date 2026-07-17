namespace Shtbly.ViewModels
{
    public class AdminSettingsViewModel
    {
        // ── Profile Tab ──────────────────────────────────────────────────────────
        public class UpdateProfileViewModel
        {
            [Required(ErrorMessage = "Name is required")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2–100 characters")]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Enter a valid email address")]
            [Display(Name = "Email Address")]
            public string Email { get; set; } = string.Empty;

            // Read-only – shown in the form but not updated here
            public string? CurrentName { get; set; }
            public string? CurrentEmail { get; set; }
        }

        // ── Password Tab ─────────────────────────────────────────────────────────
        public class ChangePasswordViewModel
        {
            [Required(ErrorMessage = "Current password is required")]
            [DataType(DataType.Password)]
            [Display(Name = "Current Password")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "New password is required")]
            [StringLength(100, MinimumLength = 8,
                ErrorMessage = "Password must be at least 8 characters")]
            [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
                ErrorMessage = "Must contain an uppercase letter, a number, and a special character")]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your new password")]
            [DataType(DataType.Password)]
            [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
            [Display(Name = "Confirm New Password")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // ── Composite wrapper sent to the View ───────────────────────────────────
        public class AdminSettingsPageViewModel
        {
            public UpdateProfileViewModel Profile { get; set; } = new();
            public ChangePasswordViewModel Password { get; set; } = new();

            /// "profile" | "password"
            public string ActiveTab { get; set; } = "profile";

            public string? SuccessMessage { get; set; }
            public string? ErrorMessage { get; set; }
        }
    }
}
