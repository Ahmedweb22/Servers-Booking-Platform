using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.CurrentWorkerService1;
using Shatbly.Services.WithdrawalService;

namespace Shatbly.Areas.Worker.Controllers
{

    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = SD.ROLE_WORKER)]
    public class WithdrawalController : Controller
    {
        private readonly IWithdrawalService _withdrawalService;
        private readonly ICurrentWorkerService _currentWorkerService;
        private readonly IEarningsService _earningsService;

        public WithdrawalController(
            IWithdrawalService withdrawalService,
            ICurrentWorkerService currentWorkerService,
            IEarningsService earningsService)
        {
            _withdrawalService = withdrawalService;
            _currentWorkerService = currentWorkerService;
            _earningsService = earningsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var workerId = await _currentWorkerService.GetCurrentWorkerIdAsync(User);

            if (workerId is null)
            {
                return NotFound("Worker profile was not found.");
            }

            var model = await _withdrawalService.GetRequestsAsync(workerId.Value);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var workerId = await _currentWorkerService.GetCurrentWorkerIdAsync(User);

            if (workerId is null)
            {
                return NotFound("Worker profile was not found.");
            }

            var dashboard = await _earningsService.GetDashboardAsync(workerId.Value);

            return View(new WithdrawalRequestVM
            {
                AvailableBalance = dashboard.PendingBalance
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WithdrawalRequestVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var workerId = await _currentWorkerService.GetCurrentWorkerIdAsync(User);

            if (workerId is null)
            {
                return NotFound("Worker profile was not found.");
            }

            var result = await _withdrawalService.CreateRequestAsync(workerId.Value, model.Amount);

            //if (!result.Succeeded)
            //{
            //    ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            //    return View(model);
            //}
            if (!result.Succeeded)
            {
                var dashboard = await _earningsService.GetDashboardAsync(workerId.Value);
                model.AvailableBalance = dashboard.PendingBalance;

                ModelState.AddModelError(string.Empty, result.ErrorMessage!);
                return View(model);
            }

            TempData["Success"] = "Withdrawal request created successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
