using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Shtbly.Models;
using Shtbly.Utilities;
using Shtbly.ViewModels;
using System;
using System.Threading.Tasks;
using Shtbly.Services.File_Service;

namespace Shtbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]
    public class ProfileController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IStringLocalizer<ProfileController> _localizer;
        private readonly IFileService _fileService;

        public ProfileController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IStringLocalizer<ProfileController> localizer,
            IFileService fileService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _localizer = localizer;
            _fileService = fileService;
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
                Email = user.Email,
                ProfilePictureUrl = user.ProfilePicture
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
            model.ProfilePictureUrl = user.ProfilePicture;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if phone number is already registered by another user
            if (user.Phone != model.Phone)
            {
                var phoneExists = await _userManager.Users.AnyAsync(u => u.Phone == model.Phone && u.Id != user.Id);
                if (phoneExists)
                {
                    ModelState.AddModelError("Phone", _localizer["PhoneAlreadyExists"].Value);
                    return View(model);
                }
            }

            // Handle Profile Picture Upload
            if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
            {
                var uploadResult = await _fileService.UploadFileAsync(
                    model.ProfilePictureFile,
                    "uploads/profiles",
                    5 * 1024 * 1024,
                    new[] { ".jpg", ".jpeg", ".png" }
                );

                if (uploadResult.Succeeded)
                {
                    user.ProfilePicture = uploadResult.FilePath;
                }
                else
                {
                    ModelState.AddModelError("ProfilePictureFile", uploadResult.ErrorMessage);
                    return View(model);
                }
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
