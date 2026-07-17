using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shtbly.Services.WithdrawalService;
using Shtbly.Utilities;

namespace Shtbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN}")]
    public class WithdrawalController : Controller
    {
        private readonly IWithdrawalService _withdrawalService;

        public WithdrawalController(IWithdrawalService withdrawalService)
        {
            _withdrawalService = withdrawalService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var requests = await _withdrawalService.GetAllRequestsAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _withdrawalService.ApproveRequestAsync(id);
            if (!result.Succeeded)
            {
                TempData["error-notification"] = result.ErrorMessage;
            }
            else
            {
                TempData["success-notification"] = "Withdrawal request approved successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _withdrawalService.RejectRequestAsync(id);
            if (!result.Succeeded)
            {
                TempData["error-notification"] = result.ErrorMessage;
            }
            else
            {
                TempData["success-notification"] = "Withdrawal request rejected successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
