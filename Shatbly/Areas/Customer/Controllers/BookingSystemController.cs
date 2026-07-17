using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shtbly.DataAccess;
using Shtbly.Models;
using Shtbly.Services.BookingSystem;
using Shtbly.Services.Hangfire;
using Shtbly.UnitOfWork;
using Shtbly.ViewModels;
using Stripe.Checkout;
using Shtbly.Services.Receipt;
using Shtbly.Utilities;
using Shtbly.Services.Notification;
using System.IO;

namespace Shtbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]

    public class BookingSystemController : Controller
    {
        private readonly IBookingSystemService _bookingSystemService;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<BookingSystemController> _localizer;
        private readonly Shtbly.UnitOfWork.IUnitOfWork _unitOfWork;
        private readonly IReceiptService _receiptService;
        private readonly IEmailSenderWithAttachment _emailSender;
        private readonly INotificationService _notificationService;

        public BookingSystemController(
            IBookingSystemService bookingSystemService, 
            UserManager<User> userManager, 
            IStringLocalizer<BookingSystemController> localizer,
            Shtbly.UnitOfWork.IUnitOfWork unitOfWork,
            IReceiptService receiptService,
            IEmailSenderWithAttachment emailSender,
            INotificationService notificationService)
        {
            _bookingSystemService = bookingSystemService;
            _userManager = userManager;
            _localizer = localizer;
            _unitOfWork = unitOfWork;
            _receiptService = receiptService;
            _emailSender = emailSender;
            _notificationService = notificationService;
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

                int? finalWorkerProfileId = null;
                
                if (!Guid.TryParse(workerId, out _))
                {
                    var decryptedId = Shtbly.Utilities.UrlObfuscator.Decrypt(workerId);
                    if (decryptedId > 0)
                    {
                        finalWorkerProfileId = decryptedId;
                    }
                    else if (int.TryParse(workerId, out var parsedId))
                    {
                        finalWorkerProfileId = parsedId;
                    }
                }

                if (finalWorkerProfileId.HasValue)
                {
                    // It is a worker profile ID, resolve the user ID
                    var workerProfile = await _unitOfWork.WorkerProfiles.GetOneAsync(
                        expression: w => w.Id == finalWorkerProfileId.Value,
                        includes: new System.Linq.Expressions.Expression<System.Func<WorkerProfile, object>>[] { w => w.WorkerServices! },
                        tracking: false);
                        
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
                    
                    var workerProfile = await _unitOfWork.WorkerProfiles.GetOneAsync(
                        expression: w => w.UserId == workerId,
                        includes: new System.Linq.Expressions.Expression<System.Func<WorkerProfile, object>>[] { w => w.WorkerServices! },
                        tracking: false);
                        
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
<<<<<<< HEAD
            return RedirectToAction(nameof(DetailsBooking), new { id = result.BookingId.Value });
=======
            if (model.PaymentMethod == Shatbly.Models.PaymentMethods.Card)
            {
                return RedirectToAction(nameof(Pay), new { id = result.BookingId });
            }
            return RedirectToAction(nameof(DetailsBooking), new { id = result.BookingId });
>>>>>>> 7a8b45e0becd14f2764ca442aaa329841b25b7a6
        }

        [HttpGet]
        [Route("b/{id}")]
        public async Task<IActionResult> DetailsBooking(int id)
        {
            var model = await _bookingSystemService.GetDetailsAsync(id);
            if (model is null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Unauthorized();
            }

            var isCustomer = User.IsInRole(SD.ROLE_CUSTOMER);
            if (isCustomer && model.Booking.UserId != user.Id)
            {
                return Forbid();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, string scheduledAt)
        {
            var model = await _bookingSystemService.GetDetailsAsync(id);
            if (model is null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Unauthorized();
            }

            var isCustomer = User.IsInRole(SD.ROLE_CUSTOMER);
            if (isCustomer && model.Booking.UserId != user.Id)
            {
                return Forbid();
            }

            var result = await _bookingSystemService.RescheduleAsync(id, scheduledAt);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(DetailsBooking), new { id = result.BookingId });
        }
        [HttpGet]
        public async Task<IActionResult> Success(int id, string? sessionId)
        { 
            var booking = await _bookingSystemService.GetDetailsAsync(id);
            if (booking is null)
            {
                return NotFound();
            }

            var result = await _bookingSystemService.MarkAsPaidAsync(id, _userManager.GetUserId(User)!);
            if (result)
            {
                TempData["Success"] = _localizer["PaymentSuccess"].Value;
                
                try 
                {
                    // If we have a sessionId, fetch it from Stripe and save the Payment details
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        var sessionService = new Stripe.Checkout.SessionService();
                        var session = sessionService.Get(sessionId);
                        if (session != null && session.PaymentStatus == "paid")
                        {
                            var bookingToUpdate = await _unitOfWork.Bookings.GetOneAsync(
                                b => b.Id == id, 
                                new System.Linq.Expressions.Expression<System.Func<Shtbly.Models.Booking, object>>[] { b => b.Payment });
                                
                            if (bookingToUpdate != null && bookingToUpdate.Payment == null)
                            {
                                bookingToUpdate.Payment = new Shtbly.Models.Payment
                                {
                                    BookingId = id,
                                    Amount = session.AmountTotal.HasValue ? (decimal)session.AmountTotal.Value / 100m : booking.Booking.TotalPrice,
                                    Method = Shtbly.Models.PaymentMethod.Card,
                                    Status = Shtbly.Models.PaymentStatus.Paid,
                                    GatewayName = "Stripe",
                                    GatewayRef = session.Id,
                                    TransactionId = session.PaymentIntentId,
                                    PaidAt = DateTime.UtcNow
                                };
                                await _unitOfWork.CommitAsync();
                            }
                        }
                    }

                    // Reload booking with full includes to ensure we have Worker.User
                    var fullBooking = await _unitOfWork.Bookings.GetOneAsync(
                        b => b.Id == id, 
                        new System.Linq.Expressions.Expression<System.Func<Shtbly.Models.Booking, object>>[] { 
                            b => b.Client, 
                            b => b.Worker.User, 
                            b => b.Payment 
                        });
                    if (fullBooking != null)
                    {
                        var receiptPath = await _receiptService.GenerateReceiptPdfAsync(fullBooking);
                        string receiptUrl = "/receipts/" + Path.GetFileName(receiptPath);
                        TempData["ReceiptUrl"] = receiptUrl;
                        
                        await _notificationService.CreateNotificationAsync(
                            fullBooking.ClientId,
                            "Payment Receipt",
                            $"Your payment for booking #{fullBooking.Id} was successful.",
                            NotificationType.System,
                            fullBooking.Id);
                        
                        await _emailSender.SendEmailWithAttachmentAsync(
                            fullBooking.Client.Email,
                            $"Payment Receipt for Booking #{fullBooking.Id}",
                            $"<p>Thank you for your payment. Please find your receipt attached.</p>",
                            receiptPath);
                            
                        if (fullBooking.Worker?.User != null)
                        {
                            await _emailSender.SendEmailWithAttachmentAsync(
                                fullBooking.Worker.User.Email,
                                $"Payment Received for Booking #{fullBooking.Id}",
                                $"<p>A payment was made by {fullBooking.Client.FName} for booking #{fullBooking.Id}. Please find the receipt attached.</p>",
                                receiptPath);
                        }
                    }
                } 
                catch(Exception) 
                {
                    // Ignore background errors
                }
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
            var model = await _bookingSystemService.GetDetailsAsync(id);
            if (model is null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Unauthorized();
            }

            var isCustomer = User.IsInRole(SD.ROLE_CUSTOMER);
            if (isCustomer && model.Booking.UserId != user.Id)
            {
                return Forbid();
            }

            var result = await _bookingSystemService.CancelAsync(id, cancellationReason);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
            return Redirect($"/b/{Shtbly.Utilities.UrlObfuscator.Encrypt(result.BookingId)}");
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
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/Customer/BookingSystem/Success?id={id}&sessionId={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/Customer/BookingSystem/Cancel?id={id}",
                LineItems = new List<SessionLineItemOptions>
                {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(booking.Booking.TotalPrice * 100),
                        Currency = "egp",
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

            var promo = await _unitOfWork.PromotionCodes.GetOneAsync(
                expression: pc => pc.Code == code && pc.IsActive && pc.Promotion.IsActive,
                includes: new System.Linq.Expressions.Expression<System.Func<PromotionCode, object>>[] { pc => pc.Promotion },
                tracking: false);

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
        public async Task<IActionResult> GetWorkerAvailability(string workerId)
        {
            if (string.IsNullOrEmpty(workerId))
            {
                return Json(new Dictionary<string, List<string>>());
            }

            var availability = await _bookingSystemService.GetAvailableSlotsByDateAsync(workerId);
            return Json(availability);
        }


        [HttpPost]
        public async Task<IActionResult> ValidateCoupon(string code, int serviceId, decimal originalPrice)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { succeeded = false, message = _localizer["CouponRequired"]?.Value ?? "Coupon code is required." });
            }

            var coupon = await _unitOfWork.Coupons.GetOneAsync(
                expression: c => c.Code == code && c.IsActive,
                tracking: false);

            if (coupon == null)
            {
                return Json(new { succeeded = false, message = _localizer["InvalidCoupon"]?.Value ?? "Invalid coupon code." });
            }

            if (coupon.UsedCount >= coupon.MaxUses)
            {
                return Json(new { succeeded = false, message = _localizer["CouponFullyUsed"]?.Value ?? "This coupon has reached its maximum uses." });
            }

            var now = DateTime.UtcNow;
            if (coupon.ValidFrom > now)
            {
                return Json(new { succeeded = false, message = _localizer["CouponNotStarted"]?.Value ?? "This coupon is not active yet." });
            }

            if (coupon.ValidUntil < now)
            {
                return Json(new { succeeded = false, message = _localizer["CouponExpired"]?.Value ?? "This coupon has expired." });
            }

            if (coupon.CategoryId.HasValue && coupon.CategoryId.Value != serviceId)
            {
                return Json(new { succeeded = false, message = _localizer["CouponInvalidForService"]?.Value ?? "This coupon is not valid for the selected service." });
            }

            decimal discountAmount = 0;
            if (coupon.DiscountType == DiscountType.Percentage)
            {
                discountAmount = Math.Round(originalPrice * (coupon.DiscountValue / 100m), 2);
            }
            else if (coupon.DiscountType == DiscountType.FixedAmount)
            {
                discountAmount = Math.Min(coupon.DiscountValue, originalPrice);
            }

            return Json(new { 
                succeeded = true, 
                discountAmount = discountAmount, 
                couponId = coupon.Id,
                message = _localizer["CouponApplied"]?.Value ?? "Coupon applied successfully!" 
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
        //[HttpPost]
        //public async Task<IActionResult> Confirm(CreateBookingViewModel model)
        //{
        //    // ... ����� ��� Booking �� �� �� ����

        //    var deadline = DateTime.UtcNow.AddMinutes(15);

        //    var order = new Order
        //    {
        //        UserId = model.UserId,
        //        ServiceId = model.ServiceId,
        //        WorkerId = model.WorkerId,
        //        BookingId = booking.Id,
        //        ScheduledAt = model.ScheduledAt,
        //        DurationHours = model.DurationHours,
        //        AddressLine = model.AddressLine,
        //        AddressLabel = model.AddressLabel,
        //        WorkerResponseDeadlineUtc = deadline,
        //        // ... ���� ������ (PaymentMethod, ServicePrice, TotalPrice, etc.)
        //    };

        //    await unitOfWork.Orders.CreateAsync(order);
        //    await unitOfWork.CommitAsync();

        //    var jobId = jobScheduler.Schedule<CancelUnconfirmedOrderJob>(
        //        job => job.ExecuteAsync(order.Id),
        //        TimeSpan.FromMinutes(15));

        //    // ����� ���� ��� jobId - Order ������ property ������� ������
        //    // (��� �������� ���)

        //    return RedirectToAction("Details", new { id = order.Id });
        //}
    }
}
