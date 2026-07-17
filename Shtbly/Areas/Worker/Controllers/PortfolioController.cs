using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shtbly.Services.Portfolio;
using System.Security.Claims;

namespace Shtbly.Areas.Worker.Controllers
{

    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_WORKER}")]
    public class PortfolioController : Controller
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IStringLocalizer<PortfolioController> _localizer;

        public PortfolioController(IPortfolioService portfolioService, IStringLocalizer<PortfolioController> localizer)
        {
            _portfolioService = portfolioService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            if (userId is null)
            {
                return Unauthorized();
            }

            var portfolio = await _portfolioService.GetWorkerPortfolioAsync(userId);

            return View(portfolio);
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View(new UploadPortfolioVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadPortfolioVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = GetCurrentUserId();

            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _portfolioService.UploadMediaAsync(userId, model);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);
                return View(model);
            }

            TempData["Success"] = _localizer["MediaUploadedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();

            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _portfolioService.DeleteMediaAsync(userId, id);

            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? _localizer["MediaDeletedSuccess"].Value : result.ErrorMessage;

            return RedirectToAction(nameof(Index));
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
