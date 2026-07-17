using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shtbly.Services.File_Service;
using System.Security.Claims;

namespace Shtbly.Areas.Worker.Controllers
{
    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_WORKER}")]
    public class WorkerProfileController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;
        private readonly IStringLocalizer<WorkerProfileController> _localizer;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;
        private readonly IRepository<WorkerService> _workerServiceRepo;
        private readonly IRepository<ServiceCategory> _categoryRepo;
        private readonly UserManager<User> _userManager;

        public WorkerProfileController(IUnitOfWork unitOfWork, IFileService fileService,
            IStringLocalizer<WorkerProfileController> localizer, IStringLocalizer<SharedResource> sharedLocalizer,
            IRepository<WorkerService> workerServiceRepo, IRepository<ServiceCategory> categoryRepo,
            UserManager<User> userManager)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _localizer = localizer;
            _sharedLocalizer = sharedLocalizer;
            _workerServiceRepo = workerServiceRepo;
            _categoryRepo = categoryRepo;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Details()
        {
            var profile = await GetCurrentWorkerProfileAsync(tracking: false);

            if (profile is null)
            {
                return NotFound(_sharedLocalizer["WorkerProfileNotFound"].Value);
            }

            return View(MapToDetailsVm(profile));
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var profile = await GetCurrentWorkerProfileAsync(tracking: false);

            if (profile is null)
            {
                return NotFound(_sharedLocalizer["WorkerProfileNotFound"].Value);
            }

            var vm = MapToEditVm(profile);

            var categories = await _categoryRepo.GetAsync(c => c.IsActive, tracking: false);
            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("ar");
            vm.Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = isRtl ? c.NameAr : c.NameEn,
                Selected = c.Id == vm.CategoryId
            }).ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditWorkerProfileVM model)
        {
            var profile = await GetCurrentWorkerProfileAsync();

            if (profile is null)
            {
                return NotFound("Worker profile was not found for the logged-in user.");
            }

            if (model.Id != profile.Id)
            {
                return Forbid();
            }

            if (model.HourlyRate <= 0)
            {
                ModelState.AddModelError("HourlyRate", "Hourly rate must be greater than 0.");
            }

            if (!ModelState.IsValid)
            {
                var categories = await _categoryRepo.GetAsync(c => c.IsActive, tracking: false);
                var isRtl = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("ar");
                model.Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = isRtl ? c.NameAr : c.NameEn,
                    Selected = c.Id == model.CategoryId
                }).ToList();
                return View(model);
            }

            profile.Bio = model.Bio.Trim();
            profile.IsAvailable = model.IsAvailable;
            profile.AcceptsOnline = model.AcceptsOnline;

            if (!string.IsNullOrEmpty(model.Password))
            {
                var user = await _userManager.FindByIdAsync(profile.UserId);
                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                    if (!passwordResult.Succeeded)
                    {
                        foreach (var error in passwordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        var categories = await _categoryRepo.GetAsync(c => c.IsActive, tracking: false);
                        var isRtl = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("ar");
                        model.Categories = categories.Select(c => new SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = isRtl ? c.NameAr : c.NameEn,
                            Selected = c.Id == model.CategoryId
                        }).ToList();
                        return View(model);
                    }
                }
            }

            // Update or Create WorkerService
            if (profile.WorkerServices == null)
            {
                var newService = new WorkerService
                {
                    WorkerId = profile.Id,
                    CategoryId = model.CategoryId,
                    HourlyRate = model.HourlyRate,
                    IsActive = model.IsAvailable
                };
                await _workerServiceRepo.CreateAsync(newService);
            }
            else
            {
                profile.WorkerServices.CategoryId = model.CategoryId;
                profile.WorkerServices.HourlyRate = model.HourlyRate;
                profile.WorkerServices.IsActive = model.IsAvailable;
            }

            await _unitOfWork.CommitAsync();

            TempData["Success"] = _localizer["ProfileUpdatedSuccess"].Value;
            return RedirectToAction(nameof(Details));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability()
        {
            var profile = await GetCurrentWorkerProfileAsync();

            if (profile is null)
            {
                return NotFound("Worker profile was not found for the logged-in user.");
            }

            profile.IsAvailable = !profile.IsAvailable;

            if (profile.WorkerServices != null)
            {
                profile.WorkerServices.IsActive = profile.IsAvailable;
            }

            await _unitOfWork.CommitAsync();

            TempData["Success"] = profile.IsAvailable
                ? _localizer["WorkerNowAvailable"].Value
                : _localizer["WorkerNowUnavailable"].Value;

            return RedirectToAction(nameof(Details));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCv(EditWorkerProfileVM model)
        {
            var profile = await GetCurrentWorkerProfileAsync();

            if (profile is null)
            {
                return NotFound(_sharedLocalizer["WorkerProfileNotFound"].Value);
            }

            if (model.Id != profile.Id)
            {
                return Forbid();
            }

            if (model.CVFile is null)
            {
                TempData["Error"] = _localizer["ChoosePdfFile"].Value;
                return RedirectToAction(nameof(Edit));
            }

            var result = await _fileService.UploadFileAsync(
                model.CVFile,
                "uploads/cv",
                maxSizeInBytes: 5 * 1024 * 1024,
                allowedExtensions: new[] { ".pdf" });

            if (!result.Succeeded)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Edit));
            }

            profile.CVPath = result.FilePath;

            _unitOfWork.WorkerProfiles.Update(profile);
            await _unitOfWork.CommitAsync();

            TempData["Success"] = _localizer["CvUploadedSuccess"].Value;
            return RedirectToAction(nameof(Details));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(EditWorkerProfileVM model)
        {
            var profile = await GetCurrentWorkerProfileAsync();

            if (profile is null)
            {
                return NotFound(_sharedLocalizer["WorkerProfileNotFound"].Value);
            }

            if (model.Id != profile.Id)
            {
                return Forbid();
            }

            if (model.ProfilePictureFile is null)
            {
                TempData["Error"] = _localizer["ChoosePhotoFile"].ResourceNotFound ? "Please choose an image file." : _localizer["ChoosePhotoFile"].Value;
                return RedirectToAction(nameof(Details));
            }

            var result = await _fileService.UploadFileAsync(
                model.ProfilePictureFile,
                "uploads/avatars",
                maxSizeInBytes: 10 * 1024 * 1024,
                allowedExtensions: new[] { ".jpg", ".jpeg", ".png" });

            if (!result.Succeeded)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Details));
            }

            profile.ProfilePicturePath = result.FilePath;

            _unitOfWork.WorkerProfiles.Update(profile);
            await _unitOfWork.CommitAsync();

            TempData["Success"] = _localizer["ProfilePictureUploadedSuccess"].ResourceNotFound ? "Profile photo updated successfully!" : _localizer["ProfilePictureUploadedSuccess"].Value;
            return RedirectToAction(nameof(Details));
        }

        private async Task<WorkerProfile?> GetCurrentWorkerProfileAsync(bool tracking = true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _unitOfWork.WorkerProfiles.GetOneAsync(
                expression: p => p.UserId == userId,
                includes:
                [
                    p => p.User,
                    p => p.Availabilities,
                    p => p.WorkerServices,
                    p => p.WorkerServices.Category
                ],
                tracking: tracking);
        }

        private static WorkerProfileVM MapToDetailsVm(WorkerProfile profile)
        {
            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("ar");
            return new WorkerProfileVM
            {
                Id = profile.Id,
                Bio = profile.Bio,
                RatingAvg = profile.RatingAvg,
                RatingCount = profile.RatingCount,
                IsVerified = profile.IsVerified,
                IsAvailable = profile.IsAvailable,
                AcceptsOnline = profile.AcceptsOnline,
                CVPath = profile.CVPath,
                ProfilePicturePath = profile.ProfilePicturePath,
                CreatedAt = profile.CreatedAt,
                WorkerName = profile.User?.UserName ?? profile.User?.Email ?? "Worker",
                Availabilities = profile.Availabilities,
                CategoryName = profile.WorkerServices != null ? (isRtl ? profile.WorkerServices.Category?.NameAr : profile.WorkerServices.Category?.NameEn) : null,
                HourlyRate = profile.WorkerServices?.HourlyRate ?? 0
            };
        }

        private static EditWorkerProfileVM MapToEditVm(WorkerProfile profile)
        {
            return new EditWorkerProfileVM
            {
                Id = profile.Id,
                Bio = profile.Bio,
                IsAvailable = profile.IsAvailable,
                AcceptsOnline = profile.AcceptsOnline,
                ExistingCVPath = profile.CVPath,
                ExistingProfilePicturePath = profile.ProfilePicturePath,
                HourlyRate = profile.WorkerServices?.HourlyRate ?? 0,
                CategoryId = profile.WorkerServices?.CategoryId ?? 0
            };
        }
    }
}