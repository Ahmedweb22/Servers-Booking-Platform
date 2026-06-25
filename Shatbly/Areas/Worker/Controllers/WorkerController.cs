using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.Notification;
using Shatbly.Services.AI;
using System.IO;

namespace Shatbly.Areas.Worker.Controllers
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

        public WorkerController(
            UserManager<User> userManager, 
            IRepository<WorkerProfile> profileRepository, 
            IStringLocalizer<WorkerController> localizer,
            INotificationService notificationService,
            IIdValidationService idValidationService)
        {
            _userManager = userManager;
            _profileRepository = profileRepository;
            _localizer = localizer;
            _notificationService = notificationService;
            _idValidationService = idValidationService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SendCV()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
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
          
            var newFileName = Guid.NewGuid().ToString() + DateTime.UtcNow.ToString("yyyy-MM-dd") + Path.GetExtension(model.cv.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\img\\worker\\worker_cv", newFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            using (var stream = System.IO.File.Create(filePath))
            {
                model.cv.CopyTo(stream);
            }

            var newIdFileName = Guid.NewGuid().ToString() + DateTime.UtcNow.ToString("yyyy-MM-dd") + Path.GetExtension(model.IdCardPhoto.FileName);
            var idFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\img\\worker\\worker_id", newIdFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(idFilePath)!);
            using (var stream = System.IO.File.Create(idFilePath))
            {
                model.IdCardPhoto.CopyTo(stream);
            }

            var Worker = new WorkerProfile
            {
                UserId = applicationUser.Id,
                CVPath = newFileName,
                IdCardPhotoPath = newIdFileName,
                Bio = string.Empty,
            };
        
            await _profileRepository.CreateAsync(Worker);
            await _profileRepository.CommitAsync();

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
