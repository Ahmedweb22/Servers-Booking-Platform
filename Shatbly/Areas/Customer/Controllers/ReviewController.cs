using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.BookingSystem;
using Shatbly.Services.File_Service;

namespace Shatbly.Areas.Customer.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize]
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
                    TempData["Success"] = "تم تسجيل تقيمك بنجاح";
                    return RedirectToAction(nameof(BookingSystemController.DetailsBooking), new { id = reviewVM.OrderId });
                }
            }
            return View(reviewVM);
        }
        public async Task<IActionResult> RaiseDispute(int id, string reason)
        { 
            var booking = await _bookingSystemService.GetDetailsAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (booking == null || booking.Booking.UserId != userId)
            {
                TempData["Error"] = "الحجز غير موجود أو لا تملك صلاحية الوصول إليه";
                return RedirectToAction(nameof(BookingSystemController.DetailsBooking), new { id });
            }
            if (string.IsNullOrEmpty(reason))
            {
                TempData["Error"] = "يجب إدخال سبب النزاع";
                return RedirectToAction(nameof(BookingSystemController.DetailsBooking), new { id });
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
            return RedirectToAction(nameof(BookingSystemController.DetailsBooking), new { id });
        }

    }
}
