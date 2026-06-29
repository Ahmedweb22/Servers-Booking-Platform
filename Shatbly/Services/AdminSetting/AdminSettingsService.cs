//namespace Shatbly.Services.AdminSetting
//{
//    public class AdminSettingsService
//    {
//        // ── Contract ─────────────────────────────────────────────────────────────
//        public interface IAdminSettingsService
//        {
//            Task<(bool Success, string? Error)> UpdateProfileAsync(
//                string userId, UpdateProfileViewModel model);

//            Task<(bool Success, string? Error)> ChangePasswordAsync(
//                string userId, ChangePasswordViewModel model);

//            Task<AdminUser?> GetAdminAsync(string userId);
//        }

//        // ── Implementation ───────────────────────────────────────────────────────
//        public class AdminSettingsService : IAdminSettingsService
//        {
//            private readonly UserManager<AdminUser> _userManager;
//            private readonly ILogger<AdminSettingsService> _logger;

//            public AdminSettingsService(
//                UserManager<AdminUser> userManager,
//                ILogger<AdminSettingsService> logger)
//            {
//                _userManager = userManager;
//                _logger = logger;
//            }

//            // ── Fetch ─────────────────────────────────────────────────────────
//            public async Task<AdminUser?> GetAdminAsync(string userId)
//                => await _userManager.FindByIdAsync(userId);

//            // ── Update Profile (Name + Email) ─────────────────────────────────
//            public async Task<(bool Success, string? Error)> UpdateProfileAsync(
//                string userId, UpdateProfileViewModel model)
//            {
//                var user = await _userManager.FindByIdAsync(userId);
//                if (user is null)
//                    return (false, "User not found.");

//                // --- email uniqueness guard ---
//                if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
//                {
//                    var existing = await _userManager.FindByEmailAsync(model.Email);
//                    if (existing is not null && existing.Id != userId)
//                        return (false, "That email address is already in use.");

//                    // update email + username (Identity links them by default)
//                    var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
//                    if (!setEmailResult.Succeeded)
//                        return (false, JoinErrors(setEmailResult.Errors));

//                    var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Email);
//                    if (!setUserNameResult.Succeeded)
//                        return (false, JoinErrors(setUserNameResult.Errors));
//                }

//                // --- update display name ---
//                user.FullName = model.FullName.Trim();
//                user.UpdatedAt = DateTime.UtcNow;

//                var updateResult = await _userManager.UpdateAsync(user);
//                if (!updateResult.Succeeded)
//                    return (false, JoinErrors(updateResult.Errors));

//                _logger.LogInformation("Admin {UserId} updated their profile.", userId);
//                return (true, null);
//            }

//            // ── Change Password ───────────────────────────────────────────────
//            public async Task<(bool Success, string? Error)> ChangePasswordAsync(
//                string userId, ChangePasswordViewModel model)
//            {
//                var user = await _userManager.FindByIdAsync(userId);
//                if (user is null)
//                    return (false, "User not found.");

//                var result = await _userManager.ChangePasswordAsync(
//                    user,
//                    model.CurrentPassword,
//                    model.NewPassword);

//                if (!result.Succeeded)
//                    return (false, JoinErrors(result.Errors));

//                _logger.LogInformation("Admin {UserId} changed their password.", userId);
//                return (true, null);
//            }

//            // ── Helpers ───────────────────────────────────────────────────────
//            private static string JoinErrors(IEnumerable<IdentityError> errors)
//                => string.Join(" ", errors.Select(e => e.Description));
//        }
//    }
//}
