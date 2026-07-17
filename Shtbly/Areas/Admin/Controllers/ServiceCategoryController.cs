using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shtbly.Services.File_Service;

namespace Shtbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN} , {SD.ROLE_SUPER_ADMIN}")]
    public class ServiceCategoryController : Controller
    {
        private IRepository<ServiceCategory> _serviceCategoryRepository;
        private readonly IStringLocalizer<ServiceCategoryController> _localizer;
        private readonly IFileService _fileService;

        public ServiceCategoryController(
            IRepository<ServiceCategory> categoryRepository,
            IStringLocalizer<ServiceCategoryController> localizer,
            IFileService fileService)
        {
            _serviceCategoryRepository = categoryRepository;
            _localizer = localizer;
            _fileService = fileService;
        }

        public async Task<IActionResult> Index(string? name, int page = 1)
        {
            var serviceCategories = await _serviceCategoryRepository.GetAsync(tracking: false);
            if (name is not null)
            {
                serviceCategories = serviceCategories.Where(c => c.NameAr.Contains(name) || c.NameEn.Contains(name)).ToList();
            }

            if (page < 1) page = 1;

            int currentPage = page;
            double totalPages = Math.Ceiling(serviceCategories.Count() / 5.0);
            serviceCategories = serviceCategories.Skip((page - 1) * 5).Take(5).ToList();

            return View(new ServiceCategoriesVM
            {
                ServiceCategories = serviceCategories.AsEnumerable(),
                CurrentPage = currentPage,
                TotalPages = totalPages
            });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ServiceCategory serviceCategory, IFormFile icon)
        {
            if (!ModelState.IsValid)
            {
                return View(serviceCategory);
            }

            if (icon is null || icon.Length == 0)
            {
                ModelState.AddModelError(nameof(ServiceCategory.Icon), "Service category icon is required.");
                return View(serviceCategory);
            }

            var upload = await _fileService.UploadFileAsync(
                icon,
                "img/service_categories",
                2 * 1024 * 1024,
                [".jpg", ".jpeg", ".png", ".gif", ".webp"]);

            if (!upload.Succeeded || string.IsNullOrWhiteSpace(upload.FilePath))
            {
                ModelState.AddModelError(nameof(ServiceCategory.Icon), upload.ErrorMessage ?? "Invalid service category icon.");
                return View(serviceCategory);
            }

            serviceCategory.Icon = Path.GetFileName(upload.FilePath);

            await _serviceCategoryRepository.CreateAsync(serviceCategory);
            await _serviceCategoryRepository.CommitAsync();

            TempData["Notification"] = _localizer["CategoryCreatedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var serviceCategory = await _serviceCategoryRepository.GetOneAsync(c => c.Id == id);
            if (serviceCategory is null)
            {
                return NotFound();
            }
            return View(serviceCategory);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ServiceCategory serviceCategory, IFormFile? icon)
        {
            if (!ModelState.IsValid)
            {
                return View(serviceCategory);
            }
            var serviceCategoryInDB = await _serviceCategoryRepository.GetOneAsync(e => e.Id == serviceCategory.Id, tracking: false);

            if (serviceCategoryInDB is null) return NotFound();

            if (icon is not null && icon.Length > 0)
            {
                var upload = await _fileService.UploadFileAsync(
                    icon,
                    "img/service_categories",
                    2 * 1024 * 1024,
                    [".jpg", ".jpeg", ".png", ".gif", ".webp"]);

                if (!upload.Succeeded || string.IsNullOrWhiteSpace(upload.FilePath))
                {
                    ModelState.AddModelError(nameof(ServiceCategory.Icon), upload.ErrorMessage ?? "Invalid service category icon.");
                    return View(serviceCategory);
                }

                DeleteCategoryIcon(serviceCategoryInDB.Icon);

                serviceCategory.Icon = Path.GetFileName(upload.FilePath);
            }
            else
            {
                serviceCategory.Icon = serviceCategoryInDB.Icon;
            }
            _serviceCategoryRepository.Update(serviceCategory);
            await _serviceCategoryRepository.CommitAsync();

            TempData["Notification"] = _localizer["CategoryUpdatedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var serviceCategory = await _serviceCategoryRepository.GetOneAsync(c => c.Id == id);
            if (serviceCategory is null)
            {
                return NotFound();
            }

            DeleteCategoryIcon(serviceCategory.Icon);

            _serviceCategoryRepository.Delete(serviceCategory);
            await _serviceCategoryRepository.CommitAsync();

            TempData["Notification"] = _localizer["CategoryDeletedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        private static void DeleteCategoryIcon(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var safeFileName = Path.GetFileName(fileName);
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "service_categories", safeFileName);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
    }
}
