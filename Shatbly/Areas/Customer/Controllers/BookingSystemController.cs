using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shatbly.Services.BookingSystem;
using Stripe.Checkout;
using Shatbly.DataAccess;
using Shatbly.Models;
using Shatbly.ViewModels;

namespace Shatbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]

    public class BookingSystemController : Controller
    {
        private readonly IBookingSystemService _bookingSystemService;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<BookingSystemController> _localizer;
        private readonly ApplicationDbContext _context;

        public BookingSystemController(
            IBookingSystemService bookingSystemService, 
            UserManager<User> userManager, 
            IStringLocalizer<BookingSystemController> localizer,
            ApplicationDbContext context)
        {
            _bookingSystemService = bookingSystemService;
            _userManager = userManager;
            _localizer = localizer;
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> CreateBooking(string? workerId)
        {
            var model = new BookingWizardViewModel();

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                model.CustomerName = user.Name ?? $"{user.FName} {user.LName}".Trim();
                model.CustomerEmail = user.Email ?? "";
                model.CustomerPhone = user.Phone ?? user.PhoneNumber ?? "";
            }

            if (!string.IsNullOrEmpty(workerId))
            {
                ViewBag.IsWorkerLocked = true;

                if (int.TryParse(workerId, out var workerProfileId))
                {
                    // It is a worker profile ID, resolve the user ID
                    var workerProfile = await _context.WorkerProfiles
                        .Include(w => w.WorkerServices)
                        .FirstOrDefaultAsync(w => w.Id == workerProfileId);
                        
                    if (workerProfile != null)
                    {
                        model.WorkerId = workerProfile.UserId;
                        if (workerProfile.WorkerServices != null)
                        {
                            model.ServiceId = workerProfile.WorkerServices.CategoryId;
                        }
                    }
                }
                else
                {
                    // It is a string User ID
                    model.WorkerId = workerId;
                    
                    var workerProfile = await _context.WorkerProfiles
                        .Include(w => w.WorkerServices)
                        .FirstOrDefaultAsync(w => w.UserId == workerId);
                        
                    if (workerProfile != null && workerProfile.WorkerServices != null)
                    {
                        model.ServiceId = workerProfile.WorkerServices.CategoryId;
                    }
                }
            }

            var vm = await _bookingSystemService.BuildCreateViewModelAsync(model);
            return View(vm);
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
        [HttpGet]
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
        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            TempData["Error"] = _localizer["PaymentFailed"].Value;
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
                            Name = string.Format(_localizer["TechnicalServiceFrom"].Value, booking.Booking.User?.Name ?? "Customer"),
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

        [HttpPost]
        public async Task<IActionResult> ValidatePromoCode(string code, int serviceId, decimal originalPrice)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { succeeded = false, message = _localizer["PromoCodeRequired"]?.Value ?? "Promo code is required." });
            }

            var promo = await _context.PromotionCodes
                .Include(pc => pc.Promotion)
                .FirstOrDefaultAsync(pc => pc.Code == code && pc.IsActive && pc.Promotion.IsActive);

            if (promo == null)
            {
                return Json(new { succeeded = false, message = _localizer["InvalidPromoCode"]?.Value ?? "Invalid promo code." });
            }

            var promotion = promo.Promotion;
            if (promo.UsedCount >= promo.MaxUses)
            {
                return Json(new { succeeded = false, message = _localizer["PromoCodeFullyUsed"]?.Value ?? "This promo code has reached its maximum uses." });
            }

            var now = DateTime.UtcNow;
            if (promotion.StartDate.HasValue && promotion.StartDate.Value > now)
            {
                return Json(new { succeeded = false, message = _localizer["PromoCodeNotStarted"]?.Value ?? "This promo code is not active yet." });
            }

            if (promotion.EndDate.HasValue && promotion.EndDate.Value < now)
            {
                return Json(new { succeeded = false, message = _localizer["PromoCodeExpired"]?.Value ?? "This promo code has expired." });
            }

            if (promotion.MinOrderValue > originalPrice)
            {
                return Json(new { succeeded = false, message = string.Format(_localizer["PromoMinOrderValue"]?.Value ?? "Minimum order value of EGP {0} is required.", promotion.MinOrderValue) });
            }

            if (promotion.CategoryId.HasValue && promotion.CategoryId.Value != serviceId)
            {
                return Json(new { succeeded = false, message = _localizer["PromoCodeInvalidForService"]?.Value ?? "This promo code is not valid for the selected service." });
            }

            decimal discountAmount = 0;
            if (promotion.DiscountType == DiscountType.Percentage)
            {
                discountAmount = Math.Round(originalPrice * (promotion.DiscountValue / 100m), 2);
            }
            else if (promotion.DiscountType == DiscountType.FixedAmount)
            {
                discountAmount = Math.Min(promotion.DiscountValue, originalPrice);
            }

            return Json(new { 
                succeeded = true, 
                discountAmount = discountAmount, 
                promoCodeId = promo.Id,
                message = _localizer["PromoCodeApplied"]?.Value ?? "Promo code applied successfully!" 
            });
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }
            var orders = await _bookingSystemService.GetCustomerOrdersAsync(user.Id);
            return View(orders);
        }
    }
}
