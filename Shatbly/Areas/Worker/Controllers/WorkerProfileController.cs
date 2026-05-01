using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.File_Service;
using System.Security.Claims;
namespace Shatbly.Areas.Worker.Controllers
{
    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = SD.ROLE_WORKER)]
    public class WorkerProfileController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;

        public WorkerProfileController(IUnitOfWork unitOfWork, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Details()
        {
            var profile = await GetCurrentWorkerProfileAsync(tracking: false);

            if (profile is null)
            {
                return NotFound("Worker profile was not found for the logged-in user.");
            }

            return View(MapToDetailsVm(profile));
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var profile = await GetCurrentWorkerProfileAsync(tracking: false);

            if (profile is null)
            {
                return NotFound("Worker profile was not found for the logged-in user.");
            }

            return View(MapToEditVm(profile));
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

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            profile.Bio = model.Bio.Trim();
            profile.IsAvailable = model.IsAvailable;
            profile.AcceptsOnline = model.AcceptsOnline;

            _unitOfWork.WorkerProfiles.Update(profile);
            await _unitOfWork.CommitAsync();

            TempData["Success"] = "Worker profile updated successfully.";
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

            _unitOfWork.WorkerProfiles.Update(profile);
            await _unitOfWork.CommitAsync();

            TempData["Success"] = profile.IsAvailable
                ? "Worker is now available."
                : "Worker is now unavailable.";

            return RedirectToAction(nameof(Details));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCv(EditWorkerProfileVM model)
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

            if (model.CVFile is null)
            {
                TempData["Error"] = "Please choose a PDF file.";
                return RedirectToAction(nameof(Edit));
            }

            var result = await _fileService.UploadPdfAsync(
                model.CVFile,
                "uploads/cv",
                maxSizeInBytes: 5 * 1024 * 1024);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Edit));
            }

            profile.CVPath = result.FilePath;

            _unitOfWork.WorkerProfiles.Update(profile);
            await _unitOfWork.CommitAsync();

            TempData["Success"] = "CV uploaded successfully.";
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
                    p => p.User
                ],
                tracking: tracking);
        }

        private static WorkerProfileVM MapToDetailsVm(WorkerProfile profile)
        {
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
                CreatedAt = profile.CreatedAt,
                WorkerName = profile.User?.UserName ?? profile.User?.Email ?? "Worker"
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
                ExistingCVPath = profile.CVPath
            };
        }
    }

}