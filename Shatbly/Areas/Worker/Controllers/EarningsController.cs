using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.CurrentWorkerService1;

namespace Shatbly.Areas.Worker.Controllers
{

    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = SD.ROLE_WORKER)]
    public class EarningsController : Controller
    {
        private readonly IEarningsService _earningsService;
        private readonly ICurrentWorkerService _currentWorkerService;

        public EarningsController(
            IEarningsService earningsService,
            ICurrentWorkerService currentWorkerService)
        {
            _earningsService = earningsService;
            _currentWorkerService = currentWorkerService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var workerId = await _currentWorkerService.GetCurrentWorkerIdAsync(User);

            if (workerId is null)
            {
                return NotFound("Worker profile was not found.");
            }

            var model = await _earningsService.GetDashboardAsync(workerId.Value);
            return View(model);
        }
    }
}
