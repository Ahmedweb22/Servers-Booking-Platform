using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Shatbly.Models;
using Shatbly.Utilities;
using Shatbly.ViewModels;
using System;
using System.Threading.Tasks;

namespace Shatbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]
    public class ProfileController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IStringLocalizer<ProfileController> _localizer;

        public ProfileController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IStringLocalizer<ProfileController> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var model = new CustomerProfileVM
            {
                FName = user.FName,
                LName = user.LName,
                Phone = user.Phone,
                Address = user.Address,
                Email = user.Email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CustomerProfileVM model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // In display-only fields, keep the original values
            model.Email = user.Email;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user.FName = model.FName;
            user.LName = model.LName;
            user.Name = model.FName + " " + model.LName;
            user.Phone = model.Phone;
            user.Address = model.Address;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Refresh the security stamp & sign-in cookie to reflect any claims changes (like name)
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = _localizer["ProfileUpdatedSuccess"].Value;
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}
