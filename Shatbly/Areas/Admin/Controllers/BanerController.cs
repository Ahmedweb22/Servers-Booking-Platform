using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shtbly.Services.File_Service;

namespace Shtbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN} , {SD.ROLE_SUPER_ADMIN}")]

    public class BanerController : Controller
    {
        private readonly IRepository<Banner> _bannerRepo;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<BanerController> _localizer;
        private readonly IFileService _fileService;

        public BanerController(
            IRepository<Banner> bannerRepo,
            UserManager<User> userManager,
            IStringLocalizer<BanerController> localizer,
            IFileService fileService)
        {
            _bannerRepo = bannerRepo;
            _userManager = userManager;
            _localizer = localizer;
            _fileService = fileService;
        }

        public async Task<IActionResult> Index(string? title, int page = 1)
        {

            var banners = await _bannerRepo.GetAsync(tracking: false);
            //Add new filter
            if (title is not null)
                banners = banners.Where(e => e.Title.Contains(title)).ToList();

            // Pagination
            if (page < 1)
                page = 1;
            int pageSize = 5;
            int currentPage = page;
            double totalCount = Math.Ceiling(banners.Count() / (double)pageSize);
            banners = banners.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return View(new BannersVM
            {
                Banners = banners.AsEnumerable(),
                CurrentPage = currentPage,
                TotalPages = totalCount
            });
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(Banner banner, IFormFile img)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            if (img == null)
            {
                ModelState.AddModelError("ImageUrl", _localizer["BannerImageRequired"].Value);
            }
            if (!ModelState.IsValid)
                return View(banner);

            if (img != null && img.Length > 0)
            {
                var upload = await _fileService.UploadFileAsync(
                    img,
                    "img/banners",
                    5 * 1024 * 1024,
                    [".jpg", ".jpeg", ".png", ".gif", ".webp"]);

                if (!upload.Succeeded || string.IsNullOrWhiteSpace(upload.FilePath))
                {
                    ModelState.AddModelError("ImageUrl", upload.ErrorMessage ?? _localizer["PleaseUploadImage"].Value);
                    return View(banner);
                }

                banner.ImageUrl = Path.GetFileName(upload.FilePath);
            }
            else
            {
                ModelState.AddModelError("ImageUrl", _localizer["PleaseUploadImage"].Value);
                return View(banner);
            }

            if (string.IsNullOrEmpty(banner.UserId))
            {
                banner.UserId = _userManager.GetUserId(User);
            }

            await _bannerRepo.CreateAsync(banner);
            await _bannerRepo.CommitAsync();
            TempData["Notification"] = _localizer["BannerCreatedSuccess"].Value;

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        [Authorize(Roles = $"{SD.ROLE_ADMIN} , {SD.ROLE_SUPER_ADMIN}")]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var banner = await _bannerRepo.GetOneAsync(e => e.Id == id);
            if (banner is null)
                return NotFound();
            return View(banner);
        }
        [HttpPost]
        [Authorize(Roles = $" {SD.ROLE_SUPER_ADMIN}")]
        public async Task<IActionResult> Edit(Banner banner, IFormFile? img)
        {
            ModelState.Remove("User");
            ModelState.Remove("UserId");

            if (!ModelState.IsValid)
                return View(banner);

            Banner? existingBanner = await _bannerRepo.GetOneAsync(e => e.Id == banner.Id, tracking: false);

            if (existingBanner is null)
                return NotFound();

            if (img != null && img.Length > 0)
            {
                var upload = await _fileService.UploadFileAsync(
                    img,
                    "img/banners",
                    5 * 1024 * 1024,
                    [".jpg", ".jpeg", ".png", ".gif", ".webp"]);

                if (!upload.Succeeded || string.IsNullOrWhiteSpace(upload.FilePath))
                {
                    ModelState.AddModelError("ImageUrl", upload.ErrorMessage ?? _localizer["PleaseUploadImage"].Value);
                    return View(banner);
                }

                if (!string.IsNullOrEmpty(existingBanner.ImageUrl))
                {
                    DeleteBannerImage(existingBanner.ImageUrl);
                }

                banner.ImageUrl = Path.GetFileName(upload.FilePath);
            }
            else
            {
                banner.ImageUrl = existingBanner.ImageUrl;
            }
            banner.UserId = existingBanner.UserId;

            _bannerRepo.Update(banner);
            await _bannerRepo.CommitAsync();
            TempData["Notification"] = _localizer["BannerUpdatedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = $" {SD.ROLE_SUPER_ADMIN}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var banner = await _bannerRepo.GetOneAsync(e => e.Id == id);
            if (banner is null)
                return NotFound();
            DeleteBannerImage(banner.ImageUrl);
            _bannerRepo.Delete(banner);
            await _bannerRepo.CommitAsync();
            TempData["Notification"] = _localizer["BannerDeletedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        private static void DeleteBannerImage(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var safeFileName = Path.GetFileName(fileName);
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "banners", safeFileName);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
    }
}
