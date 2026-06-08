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
        public ReviewController(IBookingSystemService bookingSystemService, IFileService fileService)
        {
            _bookingSystemService = bookingSystemService;
            _fileService = fileService;
        }

        public async Task<IActionResult> SubmitReview(ReviewVM reviewVM)
        {
            var bookingDetails = await _bookingSystemService.GetDetailsAsync(reviewVM.OrderId);
            if (bookingDetails == null)
            {
                TempData["Error"] = "الطلب غير موجود";
                return RedirectToAction("Index", "Home");
            }
            var order = bookingDetails.Booking;

            ViewBag.WorkerName = order.Worker?.Name ?? "غير محدد";
            ViewBag.ServiceName = order.Service?.Name ?? "غير محدد";

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
                    AfterImageUrl = afterUrl
                };
                var result = await _bookingSystemService.AddReviewAsync(review);
                if(result)
                {
                    TempData["Success"] = "تم تسجيل تقييمك بنجاح";
                    return RedirectToAction("DetailsBooking", "BookingSystem", new { id = reviewVM.OrderId });
                }
                else
                {
                    ModelState.AddModelError("", "فشل تسجيل التقييم. تأكد من إتمام الطلب أولاً.");
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
                TempData["Error"] = "الحجز غير موجود أو لا تملك صلاحية الوصول إليه";
                return RedirectToAction("DetailsBooking", "BookingSystem", new { id });
            }

            ViewBag.WorkerName = bookingDetails.Booking.Worker?.Name ?? "غير محدد";
            ViewBag.ServiceName = bookingDetails.Booking.Service?.Name ?? "غير محدد";
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
                TempData["Error"] = "الحجز غير موجود أو لا تملك صلاحية الوصول إليه";
                return RedirectToAction("DetailsBooking", "BookingSystem", new { id });
            }
            if (string.IsNullOrEmpty(reason))
            {
                TempData["Error"] = "يجب إدخال سبب النزاع";
                return RedirectToAction(nameof(RaiseDispute), new { id });
            }
            var result = await _bookingSystemService.RaiseDisputeAsync(id, reason);
            if (result)
            {
                TempData["Success"] = "تم رفع النزاع بنجاح";
            }
            else
            {
                TempData["Error"] = "فشل في رفع النزاع. يرجى المحاولة مرة أخرى.";
            }
            return RedirectToAction("DetailsBooking", "BookingSystem", new { id });
        }

    }
}
