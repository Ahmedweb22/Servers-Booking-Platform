using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.BookingSystem;
using Shatbly.Services.File_Service;
using Shatbly.Models;
using Shatbly.ViewModels;

namespace Shatbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]
    public class ReviewController : Controller
    {
        private readonly IBookingSystemService _bookingSystemService;
        private readonly IFileService _fileService;
        private readonly IStringLocalizer<ReviewController> _localizer;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;
        public ReviewController(IBookingSystemService bookingSystemService, IFileService fileService,
            IStringLocalizer<ReviewController> localizer, IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _bookingSystemService = bookingSystemService;
            _fileService = fileService;
            _localizer = localizer;
            _sharedLocalizer = sharedLocalizer;
        }

        public async Task<IActionResult> SubmitReview(ReviewVM reviewVM)
        {
            var bookingDetails = await _bookingSystemService.GetDetailsAsync(reviewVM.OrderId);
            if (bookingDetails == null)
            {
                TempData["Error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction("Index", "Home");
            }
            var order = bookingDetails.Booking;

            ViewBag.WorkerName = order.Worker?.Name ?? _sharedLocalizer["Unspecified"].Value;
            ViewBag.ServiceName = order.Service?.Name ?? _sharedLocalizer["Unspecified"].Value;

            if (ModelState.IsValid)
            {
                string beforeUrl = null;
                string afterUrl = null;
                if (reviewVM.BeforeImage != null && reviewVM.BeforeImage.Length > 0)
                {
                    var uploadResult1 = await _fileService.UploadFileAsync(reviewVM.BeforeImage, "uploads/reviews", 5 * 1024 * 1024, new[] { ".jpg", ".jpeg", ".png" });
                    if (uploadResult1.Succeeded)
                    {
                        beforeUrl = uploadResult1.FilePath;
                    }
                    else
                    {
                        ModelState.AddModelError("BeforeImage", uploadResult1.ErrorMessage);
                        return View(reviewVM);
                    }
                }
                if (reviewVM.AfterImage != null && reviewVM.AfterImage.Length > 0)
                {
                    var uploadResult2 = await _fileService.UploadFileAsync(reviewVM.AfterImage, "uploads/reviews", 5 * 1024 * 1024, new[] { ".jpg", ".jpeg", ".png" });
                    if (uploadResult2.Succeeded)
                    {
                        afterUrl = uploadResult2.FilePath;
                    }
                    else
                    {
                        ModelState.AddModelError("AfterImage", uploadResult2.ErrorMessage);
                        return View(reviewVM);
                    }
                }
                var review = new Review
                {
                    OrderId = reviewVM.OrderId,
                    BookingId = reviewVM.OrderId, // AddReviewAsync matches o.Id with review.BookingId
                    CategoryId = order.ServiceId,
                    Direction = ReviewDirection.ClientToWorker,
                    Rating = reviewVM.Rating,
                    Comment = reviewVM.Comment,
                    RevieweeId = reviewVM.RevieweeId,
                    ReviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    BeforeImageUrl = beforeUrl,
                    AfterImageUrl = afterUrl,
                    IsApproved = false,
                    WorkerProfileId = order.Booking?.WorkerId
                };
                var result = await _bookingSystemService.AddReviewAsync(review);
                if(result)
                {
                    TempData["Success"] = _localizer["ReviewSuccess"].Value;
                    return RedirectToAction("DetailsBooking", "BookingSystem", new { id = reviewVM.OrderId });
                }
                else
                {
                    ModelState.AddModelError("", _localizer["ReviewFailed"].Value);
                }
            }
            return View(reviewVM);
        }
        [HttpGet]
        public async Task<IActionResult> RaiseDispute(int id)
        {
            var bookingDetails = await _bookingSystemService.GetDetailsAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (bookingDetails == null || bookingDetails.Booking.UserId != userId)
            {
                TempData["Error"] = _localizer["BookingNotFoundOrNoAccess"].Value;
                return RedirectToAction("DetailsBooking", "BookingSystem", new { id });
            }

            ViewBag.WorkerName = bookingDetails.Booking.Worker?.Name ?? _sharedLocalizer["Unspecified"].Value;
            ViewBag.ServiceName = bookingDetails.Booking.Service?.Name ?? _sharedLocalizer["Unspecified"].Value;
            ViewBag.OrderId = id;

            return View();
        }

        [HttpPost]
        [ActionName("RaiseDispute")]
        public async Task<IActionResult> RaiseDisputePost(int id, string reason)
        { 
            var booking = await _bookingSystemService.GetDetailsAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (booking == null || booking.Booking.UserId != userId)
            {
                TempData["Error"] = _localizer["BookingNotFoundOrNoAccess"].Value;
                return RedirectToAction("DetailsBooking", "BookingSystem", new { id });
            }
            if (string.IsNullOrEmpty(reason))
            {
                TempData["Error"] = _localizer["DisputeReasonRequired"].Value;
                return RedirectToAction(nameof(RaiseDispute), new { id });
            }
            var result = await _bookingSystemService.RaiseDisputeAsync(id, reason, userId);
            if (result)
            {
                TempData["Success"] = _localizer["DisputeSuccess"].Value;
            }
            else
            {
                TempData["Error"] = _localizer["DisputeFailed"].Value;
            }
            return RedirectToAction("DetailsBooking", "BookingSystem", new { id });
        }

    }
}
