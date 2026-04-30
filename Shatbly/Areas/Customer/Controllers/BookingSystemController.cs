using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.BookingSystem;

namespace Shatbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    public class BookingSystemController : Controller
    {
        private readonly IBookingSystemService _bookingSystemService;
        public BookingSystemController(IBookingSystemService bookingSystemService)
        {
            _bookingSystemService = bookingSystemService;
        }

        [HttpGet]
        public async Task<IActionResult> CreateBooking()
        {
            var model = await _bookingSystemService.BuildCreateViewModelAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(BookingWizardViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(await _bookingSystemService.BuildCreateViewModelAsync(model));
            }

            var result = await _bookingSystemService.CreateAsync(model);
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
            return RedirectToAction(nameof(DetailsBooking), new { id = result.BookingId });
        }

        [HttpGet]
        public async Task<IActionResult> DetailsBooking(int id)
        {
            var model = await _bookingSystemService.GetDetailsAsync(id);
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
            var result = await _bookingSystemService.RescheduleAsync(id, scheduledAt);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(DetailsBooking), new { id = result.BookingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? cancellationReason)
        {
            var result = await _bookingSystemService.CancelAsync(id, cancellationReason);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(DetailsBooking), new { id = result.BookingId });
        }
    }
}
