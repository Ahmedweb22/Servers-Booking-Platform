using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shatbly.Areas.Worker.Controllers
{
    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_WORKER}")]

    public class WorkerController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IRepository<WorkerProfile> _profileRepository;
        private readonly IStringLocalizer<WorkerController> _localizer;
        public WorkerController(UserManager<User> userManager, IRepository<WorkerProfile> profileRepository, IStringLocalizer<WorkerController> localizer)
        {
            _userManager = userManager;
            _profileRepository = profileRepository;
            _localizer = localizer;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SendCV()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SendCV (WorkerVM model )
        {
            if (!ModelState.IsValid)
                return View(model);

            User applicationUser = new()
            {
                FName = model.FName,
                LName = model.LName,
                Email = model.Email,
                Phone = model.Phone,
            };
            await _userManager.CreateAsync(applicationUser);
          
                var newFileName = Guid.NewGuid().ToString() + DateTime.UtcNow.ToString("yyyy-MM-dd") + Path.GetExtension(model.cv.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\img\\worker\\worker_cv", newFileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    model.cv.CopyTo(stream);
                }
                var Worker = new WorkerProfile
                {
                    UserId = applicationUser.Id,
                    CVPath = newFileName,
                };
            
                await _profileRepository.CreateAsync(Worker);
                await _profileRepository.CommitAsync();
                TempData["Notification"] = _localizer["CvSubmittedSuccess"].Value;
                return RedirectToAction("Index", "Home", new { area = "Customer" });
            
        }
    }
}
