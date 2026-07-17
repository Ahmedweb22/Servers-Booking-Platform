using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Shtbly.ViewModels.AdminSettingsViewModel;
using static Shtbly.ViewModels.SettingViewModel;

namespace Shtbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN}")]
    public class AdminSettingsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AdminSettingsController(
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ─────────────────────────────────────────────────────────
        // Helper: current logged-in user id
        // ─────────────────────────────────────────────────────────
        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ─────────────────────────────────────────────────────────
        // GET  /Settings/Index?tab=profile|password
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index(string tab = "profile")
        {
            var user = await _userManager.FindByIdAsync(CurrentUserId);
            if (user is null) return NotFound();

            var vm = new ViewModels.SettingViewModel.SettingsViewModel
            {
                ActiveTab = tab,
                Profile = new ProfileViewModel
                {
                    FullName = user.FName + user.LName,
                    Email = user.Email ?? string.Empty,
                    CurrentName = user.FName + user.LName,
                    CurrentEmail = user.Email
                }
            };

            return View(vm);
        }

        // ─────────────────────────────────────────────────────────
        // POST /Settings/UpdateProfile
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel Profile)
        {
            if (!ModelState.IsValid)
                return await BuildView("profile", profileModel: Profile);

            var user = await _userManager.FindByIdAsync(CurrentUserId);
            if (user is null) return NotFound();

            // ── Email changed? ────────────────────────────────────
            bool emailChanged = !string.Equals(
                user.Email, Profile.Email, StringComparison.OrdinalIgnoreCase);

            if (emailChanged)
            {
                // Guard: email must be unique
                var existing = await _userManager.FindByEmailAsync(Profile.Email);
                if (existing is not null && existing.Id != user.Id)
                {
                    ModelState.AddModelError(
                        nameof(Profile.Email), "That email is already in use.");
                    return await BuildView("profile", profileModel: Profile);
                }

                var emailResult = await _userManager.SetEmailAsync(user, Profile.Email);
                if (!emailResult.Succeeded)
                {
                    AddErrors(emailResult);
                    return await BuildView("profile", profileModel: Profile);
                }

                // Keep UserName in sync with Email (default Identity behaviour)
                var userNameResult = await _userManager.SetUserNameAsync(user, Profile.Email);
                if (!userNameResult.Succeeded)
                {
                    AddErrors(userNameResult);
                    return await BuildView("profile", profileModel: Profile);
                }
            }

            // ── Name ──────────────────────────────────────────────
            //user.FullName = user.FName + user.LName;
            user.FName = Profile.FullName.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                AddErrors(updateResult);
                return await BuildView("profile", profileModel: Profile);
            }

            // Re-issue security stamp cookie when email changes
            if (emailChanged)
                await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index), new { tab = "profile" });
        }

        // ─────────────────────────────────────────────────────────
        // POST /Settings/ChangePassword
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(PasswordViewModel Password)
        {
            if (!ModelState.IsValid)
                return await BuildView("password", passwordModel: Password);

            var user = await _userManager.FindByIdAsync(CurrentUserId);
            if (user is null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(
                user,
                Password.CurrentPassword,
                Password.NewPassword);

            if (!result.Succeeded)
            {
                AddErrors(result);
                return await BuildView("password", passwordModel: Password);
            }

            // Refresh auth cookie so the user stays logged in
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction(nameof(Index), new { tab = "password" });
        }

        // ─────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────

        /// Rebuilds the page view after a validation failure.
        private async Task<IActionResult> BuildView(
            string tab,
            ProfileViewModel? profileModel = null,
            PasswordViewModel? passwordModel = null)
        {
            var user = await _userManager.FindByIdAsync(CurrentUserId);

            var vm = new ViewModels.SettingViewModel.SettingsViewModel
            {
                ActiveTab = tab,

                Profile = profileModel ?? new ProfileViewModel
                {
                    FullName = user?.FName + user?.LName ?? string.Empty,
                    Email = user?.Email ?? string.Empty,
                    CurrentName = user?.FName + user?.LName,
                    CurrentEmail = user?.Email
                },

                Password = passwordModel ?? new PasswordViewModel()
            };

            return View("Index", vm);
        }

        /// Pushes IdentityResult errors into ModelState.
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
