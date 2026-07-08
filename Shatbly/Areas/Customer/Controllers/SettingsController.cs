using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Shatbly.Models;
using Shatbly.Utilities;
using Shatbly.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shatbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]
    public class SettingsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IRepository<Address> _addressRepository;
        private readonly IStringLocalizer<SettingsController> _localizer;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IRepository<Address> addressRepository,
            IStringLocalizer<SettingsController> localizer,
            ILogger<SettingsController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _addressRepository = addressRepository;
            _localizer = localizer;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Access to Index rejected: User is not authenticated.");
                    return NotFound("User not found.");
                }

                var addresses = await _addressRepository.GetAsync(a => a.UserId == user.Id);

                var model = new CustomerSettingsVM
                {
                    Addresses = addresses
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading Settings Index for User: {UserName}", User?.Identity?.Name);
                TempData["Error"] = _localizer["SettingsLoadError"]?.Value ?? "An unexpected error occurred while loading settings.";
                return View(new CustomerSettingsVM());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(CustomerSettingsVM model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Access to ChangePassword rejected: User is not authenticated.");
                    return NotFound("User not found.");
                }

                // Bind addresses again in case of model error to reload the view properly
                model.Addresses = await _addressRepository.GetAsync(a => a.UserId == user.Id);

                // Validate ONLY the ChangePassword fields
                ModelState.Clear();
                TryValidateModel(model.ChangePassword, nameof(model.ChangePassword));

                if (!ModelState.IsValid)
                {
                    return View("Index", model);
                }

                var result = await _userManager.ChangePasswordAsync(user, model.ChangePassword.CurrentPassword, model.ChangePassword.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    TempData["Success"] = _localizer["PasswordChangedSuccess"].Value;
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error changing password for User: {UserName}", User?.Identity?.Name);
                ModelState.AddModelError(string.Empty, _localizer["PasswordChangeFailed"]?.Value ?? "An error occurred while changing the password. Please try again.");
                return View("Index", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(CustomerAddAddressVM model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Access to AddAddress rejected: User is not authenticated.");
                    return NotFound("User not found.");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => _localizer[e.ErrorMessage]?.Value ?? e.ErrorMessage);

                    TempData["Error"] = string.Join(", ", errors);
                    return RedirectToAction(nameof(Index));
                }

                // Load existing addresses to see if there are any
                var existingAddresses = await _addressRepository.GetAsync(a => a.UserId == user.Id);

                var address = new Address
                {
                    City = model.City.Trim(),
                    District = model.District?.Trim() ?? string.Empty,
                    Street = model.Street.Trim(),
                    Lat = model.Lat,
                    Lng = model.Lng,
                    IsDefault = model.IsDefault || existingAddresses.Count == 0, // first address is default automatically
                    UserId = user.Id
                };

                if (address.IsDefault)
                {
                    // Unset other defaults in tracked collection
                    foreach (var addr in existingAddresses)
                    {
                        if (addr.IsDefault)
                        {
                            addr.IsDefault = false;
                        }
                    }
                }

                await _addressRepository.CreateAsync(address);
                await _addressRepository.CommitAsync();

                TempData["Success"] = _localizer["AddressAddedSuccess"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding address for User: {UserName}", User?.Identity?.Name);
                TempData["Error"] = _localizer["AddressAddFailed"]?.Value ?? "An error occurred while adding the address. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int addressId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Access to SetDefaultAddress rejected: User is not authenticated.");
                    return NotFound("User not found.");
                }

                var addresses = await _addressRepository.GetAsync(a => a.UserId == user.Id);
                var targetAddress = addresses.FirstOrDefault(a => a.Id == addressId);

                if (targetAddress == null)
                {
                    _logger.LogWarning("Address {AddressId} not found or doesn't belong to User: {UserId}", addressId, user.Id);
                    return NotFound("Address not found.");
                }

                foreach (var addr in addresses)
                {
                    if (addr.Id == addressId)
                    {
                        addr.IsDefault = true;
                    }
                    else if (addr.IsDefault)
                    {
                        addr.IsDefault = false;
                    }
                }

                await _addressRepository.CommitAsync();
                TempData["Success"] = _localizer["DefaultAddressUpdated"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error setting default address {AddressId} for User: {UserName}", addressId, User?.Identity?.Name);
                TempData["Error"] = _localizer["SetDefaultAddressFailed"]?.Value ?? "An error occurred while setting the default address. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Access to DeleteAddress rejected: User is not authenticated.");
                    return NotFound("User not found.");
                }

                var addresses = await _addressRepository.GetAsync(a => a.UserId == user.Id);
                var addressToDelete = addresses.FirstOrDefault(a => a.Id == addressId);

                if (addressToDelete == null)
                {
                    _logger.LogWarning("Address {AddressId} not found or doesn't belong to User: {UserId} for deletion.", addressId, user.Id);
                    return NotFound("Address not found.");
                }

                bool wasDefault = addressToDelete.IsDefault;
                _addressRepository.Delete(addressToDelete);

                if (wasDefault)
                {
                    var remainingAddresses = addresses.Where(a => a.Id != addressId).ToList();
                    if (remainingAddresses.Count > 0)
                    {
                        remainingAddresses[0].IsDefault = true;
                    }
                }

                await _addressRepository.CommitAsync();
                TempData["Success"] = _localizer["AddressDeletedSuccess"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting address {AddressId} for User: {UserName}", addressId, User?.Identity?.Name);
                TempData["Error"] = _localizer["AddressDeleteFailed"]?.Value ?? "An error occurred while deleting the address. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
