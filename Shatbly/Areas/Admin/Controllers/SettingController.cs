using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shtbly.Models;
using Shtbly.Utilities;
using Shtbly.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace Shtbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN}")]
    public class SettingController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IStringLocalizer<SettingController> _localizer;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        public SettingController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IStringLocalizer<SettingController> localizer,
            IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _localizer = localizer;
            _sharedLocalizer = sharedLocalizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new EditUserVM
            {
                Id = user.Id,
                FName = user.FName,
                LName = user.LName,
                UserName = user.UserName,
                Email = user.Email,
                Phone = user.Phone,
                RoleName = roles.FirstOrDefault() ?? "Admin",
                Roles = Enumerable.Empty<IdentityRole>() // Roles list is not required for changing self settings
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(EditUserVM editUserVM)
        {
            // Remove validation for fields that the user shouldn't be changing or are populated manually
            ModelState.Remove("Roles");
            ModelState.Remove("RoleName");
            if (string.IsNullOrEmpty(editUserVM.Password))
            {
                ModelState.Remove("Password");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Ensure we set these properties back so the view renders properly if there are model errors
            var roles = await _userManager.GetRolesAsync(user);
            editUserVM.RoleName = roles.FirstOrDefault() ?? "Admin";
            editUserVM.Roles = Enumerable.Empty<IdentityRole>();

            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = _sharedLocalizer["InvalidData"].Value;
                return View(editUserVM);
            }

            // Duplicate checks
            var existingUserByEmail = await _userManager.FindByEmailAsync(editUserVM.Email);
            if (existingUserByEmail != null && existingUserByEmail.Id != user.Id)
            {
                ModelState.AddModelError("Email", _sharedLocalizer["EmailAlreadyExists"]?.Value ?? "Email already exists.");
            }

            var existingUserByUsername = await _userManager.FindByNameAsync(editUserVM.UserName);
            if (existingUserByUsername != null && existingUserByUsername.Id != user.Id)
            {
                ModelState.AddModelError("UserName", _sharedLocalizer["UsernameAlreadyExists"]?.Value ?? "Username already exists.");
            }

            var existingUserByPhone = await _userManager.Users.FirstOrDefaultAsync(u => u.Phone == editUserVM.Phone);
            if (existingUserByPhone != null && existingUserByPhone.Id != user.Id)
            {
                ModelState.AddModelError("Phone", _sharedLocalizer["PhoneAlreadyExists"]?.Value ?? "Phone number already exists.");
            }

            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = _sharedLocalizer["InvalidData"].Value;
                return View(editUserVM);
            }

            user.FName = editUserVM.FName;
            user.LName = editUserVM.LName;
            user.Name = editUserVM.FName + " " + editUserVM.LName;
            user.UserName = editUserVM.UserName;
            user.Email = editUserVM.Email;
            user.Phone = editUserVM.Phone;
            user.PhoneNumber = editUserVM.Phone;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                TempData["error-notification"] = _sharedLocalizer["UpdateFailed"]?.Value ?? "Update failed.";
                return View(editUserVM);
            }

            // Update Password if provided
            if (!string.IsNullOrEmpty(editUserVM.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, editUserVM.Password);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    TempData["error-notification"] = _sharedLocalizer["UpdateFailed"]?.Value ?? "Update failed.";
                    return View(editUserVM);
                }
            }

            // Refresh user session context so their security stamp is updated
            await _signInManager.RefreshSignInAsync(user);

            TempData["success-notification"] = _sharedLocalizer["UpdateSuccessful"]?.Value ?? "Settings updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
