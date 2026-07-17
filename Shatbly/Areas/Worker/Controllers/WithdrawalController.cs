using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shtbly.Services.CurrentWorkerService1;
using Shtbly.Services.WithdrawalService;

namespace Shtbly.Areas.Worker.Controllers
{

    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_WORKER}")]
    public class WithdrawalController : Controller
    {
        private readonly IWithdrawalService _withdrawalService;
        private readonly ICurrentWorkerService _currentWorkerService;
        private readonly IEarningsService _earningsService;
        private readonly IStringLocalizer<WithdrawalController> _localizer;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        public WithdrawalController(
            IWithdrawalService withdrawalService,
            ICurrentWorkerService currentWorkerService,
            IEarningsService earningsService,
            IStringLocalizer<WithdrawalController> localizer,
            IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _withdrawalService = withdrawalService;
            _currentWorkerService = currentWorkerService;
            _earningsService = earningsService;
            _localizer = localizer;
            _sharedLocalizer = sharedLocalizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var workerId = await _currentWorkerService.GetCurrentWorkerIdAsync(User);

            if (workerId is null)
            {
                return NotFound(_sharedLocalizer["WorkerProfileNotFoundShort"].Value);
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
                return NotFound(_sharedLocalizer["WorkerProfileNotFoundShort"].Value);
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
            var workerId = await _currentWorkerService.GetCurrentWorkerIdAsync(User);

            if (workerId is null)
            {
                return NotFound(_sharedLocalizer["WorkerProfileNotFoundShort"].Value);
            }

            if (!ModelState.IsValid)
            {
                var dashboard = await _earningsService.GetDashboardAsync(workerId.Value);
                model.AvailableBalance = dashboard.PendingBalance;
                return View(model);
            }

            var result = await _withdrawalService.CreateRequestAsync(workerId.Value, model.Amount);

            if (!result.Succeeded)
            {
                var dashboard = await _earningsService.GetDashboardAsync(workerId.Value);
                model.AvailableBalance = dashboard.PendingBalance;

                ModelState.AddModelError(string.Empty, result.ErrorMessage!);
                return View(model);
            }

            TempData["Success"] = _localizer["WithdrawalCreatedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }
    }
}
