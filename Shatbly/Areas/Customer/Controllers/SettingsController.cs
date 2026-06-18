using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Shatbly.Models;
using Shatbly.Utilities;
using Shatbly.ViewModels;
using System.Collections.Generic;
using System.Security.Claims;
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

        public SettingsController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IRepository<Address> addressRepository,
            IStringLocalizer<SettingsController> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _addressRepository = addressRepository;
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

            var addresses = await _addressRepository.GetAsync(a => a.UserId == user.Id);

            var model = new CustomerSettingsVM
            {
                Addresses = addresses
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(CustomerSettingsVM model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(string city, string district, string street, double? lat, double? lng, bool isDefault)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(street))
            {
                TempData["Error"] = _localizer["CityAndStreetRequired"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Load existing addresses to see if there are any
            var existingAddresses = await _addressRepository.GetAsync(a => a.UserId == user.Id);

            var address = new Address
            {
                City = city.Trim(),
                District = district?.Trim() ?? string.Empty,
                Street = street.Trim(),
                Lat = lat,
                Lng = lng,
                IsDefault = isDefault || existingAddresses.Count == 0, // first address is default automatically
                UserId = user.Id
            };

            if (address.IsDefault)
            {
                // Unset other defaults
                foreach (var addr in existingAddresses)
                {
                    if (addr.IsDefault)
                    {
                        addr.IsDefault = false;
                        _addressRepository.Update(addr);
                    }
                }
            }

            await _addressRepository.CreateAsync(address);
            await _addressRepository.CommitAsync();

            TempData["Success"] = _localizer["AddressAddedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var addresses = await _addressRepository.GetAsync(a => a.UserId == user.Id);
            Address? targetAddress = null;

            foreach (var addr in addresses)
            {
                if (addr.Id == addressId)
                {
                    addr.IsDefault = true;
                    targetAddress = addr;
                    _addressRepository.Update(addr);
                }
                else if (addr.IsDefault)
                {
                    addr.IsDefault = false;
                    _addressRepository.Update(addr);
                }
            }

            if (targetAddress == null)
            {
                return NotFound("Address not found.");
            }

            await _addressRepository.CommitAsync();
            TempData["Success"] = _localizer["DefaultAddressUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var address = await _addressRepository.GetOneAsync(a => a.Id == addressId && a.UserId == user.Id);
            if (address == null)
            {
                return NotFound("Address not found.");
            }

            bool wasDefault = address.IsDefault;
            _addressRepository.Delete(address);
            await _addressRepository.CommitAsync();

            // If we deleted the default address, set another one as default if any exists
            if (wasDefault)
            {
                var remainingAddresses = await _addressRepository.GetAsync(a => a.UserId == user.Id);
                if (remainingAddresses.Count > 0)
                {
                    remainingAddresses[0].IsDefault = true;
                    _addressRepository.Update(remainingAddresses[0]);
                    await _addressRepository.CommitAsync();
                }
            }

            TempData["Success"] = _localizer["AddressDeletedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }
    }
}
