using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.Portfolio;
using System.Security.Claims;

namespace Shatbly.Areas.Worker.Controllers
{

    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = SD.ROLE_WORKER)]
    public class PortfolioController : Controller
    {
        private readonly IPortfolioService _portfolioService;

        public PortfolioController(IPortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
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

            TempData["Success"] = "Portfolio media uploaded successfully.";
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
                result.Succeeded ? "Media deleted successfully." : result.ErrorMessage;

            return RedirectToAction(nameof(Index));
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
