using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.AvailabilityService;
using System.Security.Claims;

namespace Shatbly.Areas.Worker.Controllers;

[Area(SD.WORKER_AREA)]
[Authorize(Roles = SD.ROLE_WORKER)]
public class AvailabilityController : Controller
{
    private readonly IAvailabilityService _availabilityService;
    private readonly IUnitOfWork _unitOfWork;

    public AvailabilityController(
        IAvailabilityService availabilityService,
        IUnitOfWork unitOfWork)
    {
        _availabilityService = availabilityService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var workerProfile = await GetCurrentWorkerProfileAsync();

        if (workerProfile is null)
        {
            return NotFound("Worker profile was not found.");
        }

        var schedule = await _availabilityService.GetWorkerScheduleAsync(workerProfile.Id);

        return View(schedule);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var workerProfile = await GetCurrentWorkerProfileAsync();

        if (workerProfile is null)
        {
            return NotFound("Worker profile was not found.");
        }

        return View(new CreateAvailabilityVM
        {
            WorkerId = workerProfile.Id
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAvailabilityVM model)
    {
        var workerProfile = await GetCurrentWorkerProfileAsync();

        if (workerProfile is null)
        {
            return NotFound("Worker profile was not found.");
        }

        model.WorkerId = workerProfile.Id;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _availabilityService.AddAvailabilityAsync(model);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(model);
        }

        TempData["Success"] = "Availability slot added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var workerProfile = await GetCurrentWorkerProfileAsync();

        if (workerProfile is null)
        {
            return NotFound("Worker profile was not found.");
        }

        var schedule = await _availabilityService.GetWorkerScheduleAsync(workerProfile.Id);
        var slot = schedule.FirstOrDefault(x => x.Id == id);

        if (slot is null)
        {
            return NotFound();
        }

        return View(new CreateAvailabilityVM
        {
            Id = slot.Id,
            WorkerId = workerProfile.Id,
            DayOfWeek = slot.DayOfWeek,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateAvailabilityVM model)
    {
        var workerProfile = await GetCurrentWorkerProfileAsync();

        if (workerProfile is null)
        {
            return NotFound("Worker profile was not found.");
        }

        model.Id = id;
        model.WorkerId = workerProfile.Id;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _availabilityService.UpdateAvailabilityAsync(id, model);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(model);
        }

        TempData["Success"] = "Availability slot updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _availabilityService.DeleteAvailabilityAsync(id);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Availability slot deleted successfully." : result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }

    private async Task<WorkerProfile?> GetCurrentWorkerProfileAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _unitOfWork.WorkerProfiles.GetOneAsync(
            expression: x => x.UserId == userId,
            tracking: false);
    }
}
