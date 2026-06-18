using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.BookingSystem;
using Stripe.Checkout;

namespace Shatbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]

    public class BookingSystemController : Controller
    {
        private readonly IBookingSystemService _bookingSystemService;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<BookingSystemController> _localizer;
        public BookingSystemController(IBookingSystemService bookingSystemService, UserManager<User> userManager, IStringLocalizer<BookingSystemController> localizer)
        {
            _bookingSystemService = bookingSystemService;
            _userManager = userManager;
            _localizer = localizer;
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
        public async Task<IActionResult> Success(int id)
        { 
        var booking = await _bookingSystemService.GetDetailsAsync(id);
            if (booking is null)
            {
                return NotFound();
            }
        var result = await _bookingSystemService.MarkAsPaidAsync(id);
            if (result)
            {
                TempData["Success"] = _localizer["PaymentSuccess"].Value;

            }
            else
            {
                TempData["Error"] = _localizer["PaymentFailed"].Value;
            }

      
            return RedirectToAction(nameof(DetailsBooking), new { id = id });
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
        public async Task<IActionResult> Pay(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return NotFound();
            var booking = await _bookingSystemService.GetDetailsAsync(id);
            if (booking is null)
                return NotFound();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                Mode = "payment",
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/Customer/BookingSystem/Success?id={id}",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/Customer/BookingSystem/Cancel?id={id}",
                LineItems = new List<SessionLineItemOptions>
                {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(booking.Booking.TotalPrice * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = string.Format(_localizer["TechnicalServiceFrom"].Value, booking.Booking.User.Name),
                            Description = _localizer["MaintenanceBooking"].Value,
                        },

                    },
                    Quantity = 1,
                }
            }
            };
            var services = new SessionService();
            var session = services.Create(options);
            return Redirect(session.Url);
        }
    }
}
