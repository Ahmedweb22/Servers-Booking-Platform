using Microsoft.AspNetCore.Mvc;

using Shatbly.Services.BookingSystem;

namespace Shatbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
 
    public class BookingSystemController(IBookingSystemService bookingSystemService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = await bookingSystemService.BuildCreateViewModelAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingWizardViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(await bookingSystemService.BuildCreateViewModelAsync(model));
            }

            var result = await bookingSystemService.CreateAsync(model);
            if (!result.Succeeded)
            {
                foreach (var error in result.ValidationErrors)
                {
                    foreach (var message in error.Value)
                    {
                        ModelState.AddModelError(error.Key, message);
                    }
                }

                return View(result.ViewModel);
            }

            TempData["Success"] = result.SuccessMessage;
            return RedirectToAction(nameof(Details), new { id = result.BookingId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var model = await bookingSystemService.GetDetailsAsync(id);
            if (model is null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, string scheduledAt)
        {
            var result = await bookingSystemService.RescheduleAsync(id, scheduledAt);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = result.BookingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? cancellationReason)
        {
            var result = await bookingSystemService.CancelAsync(id, cancellationReason);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = result.BookingId });
        }
    }
}