using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shatbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    //[Authorize(Roles = $"{SD.ROLE_ADMIN} , {SD.ROLE_SUPER_ADMIN}")]
    public class ServiceCategoryController : Controller
    {
        private IRepository<ServiceCategory> _serviceCategoryRepository;
        public ServiceCategoryController(IRepository<ServiceCategory> categoryRepository)
        {
            _serviceCategoryRepository = categoryRepository;
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
            var newFileName = Guid.NewGuid().ToString() + DateTime.UtcNow.ToString("yyyy-MM-dd") + Path.GetExtension(icon.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\img\\service_categories", newFileName);
            using (var stream = System.IO.File.Create(filePath))
            {
                icon.CopyTo(stream);
            }
            serviceCategory.Icon = newFileName;

            await _serviceCategoryRepository.CreateAsync(serviceCategory);
            await _serviceCategoryRepository.CommitAsync();

            TempData["Notification"] = "Service category created successfully";
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
                var newFileName = Guid.NewGuid().ToString().Substring(0, 7) + DateTime.UtcNow.ToString("yyyy-MM-dd") + Path.GetExtension(icon.FileName);

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\img\\service_categories", newFileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    icon.CopyTo(stream);
                }

                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\img\\service_categories", serviceCategoryInDB.Icon);
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }

                serviceCategory.Icon = newFileName;
            }
            else
            {
                serviceCategory.Icon = serviceCategoryInDB.Icon;
            }
            _serviceCategoryRepository.Update(serviceCategory);
            await _serviceCategoryRepository.CommitAsync();

            TempData["Notification"] = "Service category updated successfully";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var serviceCategory = await _serviceCategoryRepository.GetOneAsync(c => c.Id == id);
            if (serviceCategory is null)
            {
                return NotFound();
            }

            var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\img\\service_categories", serviceCategory.Icon);
            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }

            _serviceCategoryRepository.Delete(serviceCategory);
            await _serviceCategoryRepository.CommitAsync();

            TempData["Notification"] = "Service category deleted successfully";
            return RedirectToAction(nameof(Index));
        }
    }
}
