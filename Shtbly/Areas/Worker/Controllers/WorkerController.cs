using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shtbly.Services.Notification;
using Shtbly.Services.AI;
using Shtbly.Services.File_Service;
using System.IO;

namespace Shtbly.Areas.Worker.Controllers
{
    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_WORKER}")]

    public class WorkerController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IRepository<WorkerProfile> _profileRepository;
        private readonly IStringLocalizer<WorkerController> _localizer;
        private readonly INotificationService _notificationService;
        private readonly IIdValidationService _idValidationService;
        private readonly IFileService _fileService;
        private readonly IRepository<Shtbly.Models.Address> _addressRepository;

        public WorkerController(
            UserManager<User> userManager, 
            IRepository<WorkerProfile> profileRepository, 
            IStringLocalizer<WorkerController> localizer,
            INotificationService notificationService,
            IIdValidationService idValidationService,
            IFileService fileService,
            IRepository<Shtbly.Models.Address> addressRepository)
        {
            _userManager = userManager;
            _profileRepository = profileRepository;
            _localizer = localizer;
            _notificationService = notificationService;
            _idValidationService = idValidationService;
            _fileService = fileService;
            _addressRepository = addressRepository;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SendCV()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCV(WorkerVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // AI verification of ID Card photo
            var idValidationResult = await _idValidationService.ValidateIdCardAsync(model.IdCardPhoto);
            if (!idValidationResult.IsValid)
            {
                ModelState.AddModelError("IdCardPhoto", idValidationResult.Reason);
                return View(model);
            }

            // Check if phone number is already registered
            var phoneExists = await _userManager.Users.AnyAsync(u => u.Phone == model.Phone);
            if (phoneExists)
            {
                ModelState.AddModelError("Phone", _localizer["PhoneAlreadyExists"].Value);
                return View(model);
            }

            // Check if email is already registered
            var emailExists = await _userManager.FindByEmailAsync(model.Email) != null;
            if (emailExists)
            {
                ModelState.AddModelError("Email", _localizer["EmailAlreadyExists"].Value);
                return View(model);
            }

            User applicationUser = new()
            {
                UserName = model.Email,
                FName = model.FName,
                LName = model.LName,
                Name = model.FName + " " + model.LName,
                Email = model.Email,
                Phone = model.Phone,
                Address = string.IsNullOrEmpty(model.District) ? $"{model.City}, {model.Address}" : $"{model.City}, {model.District}, {model.Address}"
            };
            var result = await _userManager.CreateAsync(applicationUser, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(applicationUser, SD.ROLE_WORKER);

            var cvUpload = await _fileService.UploadFileAsync(
                model.cv,
                "uploads/cv",
                maxSizeInBytes: 5 * 1024 * 1024,
                allowedExtensions: new[] { ".pdf" });

            if (!cvUpload.Succeeded)
            {
                await _userManager.DeleteAsync(applicationUser);
                ModelState.AddModelError("cv", cvUpload.ErrorMessage ?? "Invalid CV file.");
                return View(model);
            }

            var idUpload = await _fileService.UploadFileAsync(
                model.IdCardPhoto,
                "uploads/worker-id",
                maxSizeInBytes: 5 * 1024 * 1024,
                allowedExtensions: new[] { ".jpg", ".jpeg", ".png", ".webp" });

            if (!idUpload.Succeeded)
            {
                await _userManager.DeleteAsync(applicationUser);
                ModelState.AddModelError("IdCardPhoto", idUpload.ErrorMessage ?? "Invalid ID card photo.");
                return View(model);
            }

            var Worker = new WorkerProfile
            {
                UserId = applicationUser.Id,
                CVPath = cvUpload.FilePath,
                IdCardPhotoPath = idUpload.FilePath,
                Bio = string.Empty,
            };
        
            await _profileRepository.CreateAsync(Worker);
            await _profileRepository.CommitAsync();

            // Create and save worker's address
            var address = new Shtbly.Models.Address
            {
                City = model.City,
                District = model.District ?? string.Empty,
                Street = model.Address ?? string.Empty,
                Lat = model.Lat,
                Lng = model.Lng,
                IsDefault = true,
                UserId = applicationUser.Id
            };
            await _addressRepository.CreateAsync(address);
            await _addressRepository.CommitAsync();

            // Notify admins
            var admins = await _userManager.GetUsersInRoleAsync(SD.ROLE_ADMIN);
            var superAdmins = await _userManager.GetUsersInRoleAsync(SD.ROLE_SUPER_ADMIN);
            var adminsToNotify = admins.Concat(superAdmins).GroupBy(u => u.Id).Select(g => g.First()).ToList();

            foreach (var admin in adminsToNotify)
            {
                await _notificationService.CreateNotificationAsync(
                    admin.Id,
                    "New Worker Applied",
                    $"Worker {applicationUser.FName} {applicationUser.LName} ({applicationUser.Email}) has submitted a CV to join.",
                    NotificationType.System
                );
            }

            TempData["Notification"] = _localizer["CvSubmittedSuccess"].Value;
            return RedirectToAction(nameof(ApplicationSubmitted));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ApplicationSubmitted()
        {
            return View();
        }
    }
}
