using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using Shtbly.Models;
using Shtbly.ViewModels;
using Shtbly.Utilities;
using Shtbly.Services.File_Service;
using System.IO;

namespace Shtbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN}")]
    public class WorkerProfileController : Controller
    {
        private readonly IRepository<WorkerProfile> _workerProfileRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IStringLocalizer<WorkerProfileController> _localizer;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;
        private readonly IFileService _fileService;

        public WorkerProfileController(
            IRepository<WorkerProfile> workerProfileRepo,
            IRepository<User> userRepo,
            IStringLocalizer<WorkerProfileController> localizer,
            IStringLocalizer<SharedResource> sharedLocalizer,
            IFileService fileService)
        {
            _workerProfileRepo = workerProfileRepo;
            _userRepo = userRepo;
            _localizer = localizer;
            _sharedLocalizer = sharedLocalizer;
            _fileService = fileService;
        }

        // ───────── INDEX ─────────
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var includes = new Expression<Func<WorkerProfile, object>>[]
            {
                wp => wp.User
            };

            var workerProfiles = await _workerProfileRepo.GetAsync(includes: includes, tracking: false);

            // Filter by user name or email
            if (!string.IsNullOrWhiteSpace(search))
            {
                workerProfiles = workerProfiles
                    .Where(wp =>
                        (wp.User?.FName != null && wp.User.FName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (wp.User?.LName != null && wp.User.LName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (wp.User?.Email != null && wp.User.Email.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (page < 1) page = 1;
            int pageSize = 5;
            int currentPage = page;
            double totalPages = Math.Ceiling(workerProfiles.Count() / (double)pageSize);
            workerProfiles = workerProfiles.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return View(new WorkerProfilesVM
            {
                WorkerProfiles = workerProfiles,
                CurrentPage = currentPage,
                TotalPages = totalPages
            });
        }

        // ───────── CREATE (GET) ─────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new WorkerProfilesVM
            {
                Users = await _userRepo.GetAsync(tracking: false)
            };
            return View(vm);
        }

        // ───────── CREATE (POST) ─────────
        [HttpPost]
        public async Task<IActionResult> Create(WorkerProfilesVM model)
        {
            if (!ModelState.IsValid)
            {
                model.Users = await _userRepo.GetAsync(tracking: false);
                TempData["error-notification"] = _sharedLocalizer["PleaseCorrectErrors"].Value;
                return View(model);
            }

            var workerProfile = new WorkerProfile
            {
                UserId = model.UserId,
                Bio = model.Bio ?? string.Empty,
                IsVerified = model.IsVerified,
                IsAvailable = model.IsAvailable,
                AcceptsOnline = model.AcceptsOnline,
                IsApproved = model.IsApproved,
                InterviewDate = model.InterviewDate,
                HRNotes = model.HRNotes
            };

            if (model.IsApproved)
            {
                var user = await _userRepo.GetOneAsync(u => u.Id == model.UserId, tracking: true);
                if (user != null)
                {
                    user.EmailConfirmed = true;
                }
            }

            if (model.CVFile is not null)
            {
                var upload = await _fileService.UploadFileAsync(
                    model.CVFile,
                    "img/worker/worker_cv",
                    5 * 1024 * 1024,
                    [".pdf"]);

                if (!upload.Succeeded || string.IsNullOrWhiteSpace(upload.FilePath))
                {
                    model.Users = await _userRepo.GetAsync(tracking: false);
                    ModelState.AddModelError(nameof(model.CVFile), upload.ErrorMessage ?? "Invalid CV file.");
                    TempData["error-notification"] = upload.ErrorMessage ?? "Invalid CV file.";
                    return View(model);
                }

                workerProfile.CVPath = upload.FilePath;
            }

            await _workerProfileRepo.CreateAsync(workerProfile);
            await _workerProfileRepo.CommitAsync();
            TempData["success-notification"] = _localizer["WorkerProfileCreatedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        // ───────── EDIT (GET) ─────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var includes = new Expression<Func<WorkerProfile, object>>[]
            {
                wp => wp.User
            };

            var wp = await _workerProfileRepo.GetOneAsync(e => e.Id == id, includes: includes, tracking: false);
            if (wp is null)
            {
                TempData["error-notification"] = _localizer["WorkerProfileNotFound"].Value;
                return NotFound();
            }

            var vm = new WorkerProfilesVM
            {
                Id = wp.Id,
                UserId = wp.UserId,
                Bio = wp.Bio,
                IsVerified = wp.IsVerified,
                IsAvailable = wp.IsAvailable,
                AcceptsOnline = wp.AcceptsOnline,
                IsApproved = wp.IsApproved,
                InterviewDate = wp.InterviewDate,
                HRNotes = wp.HRNotes,
                ExistingCVPath = wp.CVPath,
                Users = await _userRepo.GetAsync(tracking: false)
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(WorkerProfilesVM model)
        {
            if (!ModelState.IsValid)
            {
                model.Users = await _userRepo.GetAsync(tracking: false);
                TempData["error-notification"] = _sharedLocalizer["PleaseCorrectErrors"].Value;
                return View(model);
            }

            var workerProfile = await _workerProfileRepo.GetOneAsync(
                expression: e => e.Id == model.Id,
                includes: [e => e.WorkerServices, e => e.User],
                tracking: true);

            if (workerProfile is null)
            {
                TempData["error-notification"] = _localizer["WorkerProfileNotFound"].Value;
                return NotFound();
            }

            workerProfile.UserId = model.UserId;
            workerProfile.Bio = model.Bio ?? string.Empty;
            workerProfile.IsVerified = model.IsVerified;
            workerProfile.IsAvailable = model.IsAvailable;
            workerProfile.AcceptsOnline = model.AcceptsOnline;
            workerProfile.IsApproved = model.IsApproved;
            workerProfile.InterviewDate = model.InterviewDate;
            workerProfile.HRNotes = model.HRNotes;

            if (model.IsApproved && workerProfile.User != null)
            {
                workerProfile.User.EmailConfirmed = true;
            }

            if (model.CVFile is not null)
            {
                var upload = await _fileService.UploadFileAsync(
                    model.CVFile,
                    "img/worker/worker_cv",
                    5 * 1024 * 1024,
                    [".pdf"]);

                if (!upload.Succeeded || string.IsNullOrWhiteSpace(upload.FilePath))
                {
                    model.Users = await _userRepo.GetAsync(tracking: false);
                    ModelState.AddModelError(nameof(model.CVFile), upload.ErrorMessage ?? "Invalid CV file.");
                    TempData["error-notification"] = upload.ErrorMessage ?? "Invalid CV file.";
                    return View(model);
                }
                
                DeleteCv(workerProfile.CVPath);
                
                workerProfile.CVPath = upload.FilePath;
            }

            if (workerProfile.WorkerServices != null)
            {
                workerProfile.WorkerServices.IsActive = model.IsAvailable;
            }

            await _workerProfileRepo.CommitAsync();
            TempData["success-notification"] = _localizer["WorkerProfileUpdatedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        // ───────── DELETE ─────────
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var wp = await _workerProfileRepo.GetOneAsync(e => e.Id == id);
            if (wp is null)
            {
                TempData["error-notification"] = _localizer["WorkerProfileNotFound"].Value;
                return NotFound();
            }

            if (!string.IsNullOrEmpty(wp.CVPath))
            {
                DeleteCv(wp.CVPath);
            }

            _workerProfileRepo.Delete(wp);
            await _workerProfileRepo.CommitAsync();
            TempData["success-notification"] = _localizer["WorkerProfileDeletedSuccess"].Value;
            return Ok();
        }

        private static void DeleteCv(string? relativePathOrFileName)
        {
            if (string.IsNullOrWhiteSpace(relativePathOrFileName))
            {
                return;
            }

            var cleanPath = relativePathOrFileName.Replace("\\", "/").TrimStart('/');
            if (cleanPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Contains(".."))
            {
                return;
            }

            if (!cleanPath.Contains('/'))
            {
                cleanPath = $"img/worker/worker_cv/{Path.GetFileName(cleanPath)}";
            }

            var webRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            var physicalPath = Path.GetFullPath(Path.Combine(webRoot, cleanPath));
            var webRootPrefix = webRoot.EndsWith(Path.DirectorySeparatorChar)
                ? webRoot
                : webRoot + Path.DirectorySeparatorChar;

            if (!physicalPath.StartsWith(webRootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
        }
    }
}
