using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Shatbly.Models;
using Shatbly.Repositories.IRepositories;
using Shatbly.Utilities;
using Shatbly.ViewModels;
using System.Linq.Expressions;

namespace Shatbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN}")]
    public class ReviewController : Controller
    {
        private readonly IRepository<Review> _reviewRepo;
        private readonly IRepository<Booking> _bookingRepo;
        private readonly IRepository<WorkerProfile> _workerProfileRepo;
        private readonly IStringLocalizer<ReviewController> _localizer;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        public ReviewController(
            IRepository<Review> reviewRepo,
            IRepository<Booking> bookingRepo,
            IRepository<WorkerProfile> workerProfileRepo,
            IStringLocalizer<ReviewController> localizer,
            IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _reviewRepo = reviewRepo;
            _bookingRepo = bookingRepo;
            _workerProfileRepo = workerProfileRepo;
            _localizer = localizer;
            _sharedLocalizer = sharedLocalizer;
        }

        // ───────── INDEX ─────────
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            // Auto-heal reviews that are missing WorkerProfileId
            var nullProfileReviews = await _reviewRepo.GetAsync(
                r => r.WorkerProfileId == null || r.WorkerProfileId == 0,
                tracking: true
            );
            if (nullProfileReviews.Any())
            {
                bool changed = false;
                foreach (var review in nullProfileReviews)
                {
                    var profile = await _workerProfileRepo.GetOneAsync(wp => wp.UserId == review.RevieweeId, tracking: false);
                    if (profile != null)
                    {
                        review.WorkerProfileId = profile.Id;
                        changed = true;
                    }
                }
                if (changed)
                {
                    await _reviewRepo.CommitAsync();

                    // Recalculate ratings
                    var workerUserIds = nullProfileReviews.Select(r => r.RevieweeId).Distinct().ToList();
                    foreach (var wUserId in workerUserIds)
                    {
                        await RecalculateWorkerRatingAsync(wUserId);
                    }
                }
            }

            var includes = new Expression<Func<Review, object>>[]
            {
                r => r.Reviewer,
                r => r.Reviewee,
                r => r.Category,
                r => r.Booking
            };

            var reviews = await _reviewRepo.GetAsync(includes: includes, tracking: false);

            if (!string.IsNullOrWhiteSpace(search))
            {
                reviews = reviews.Where(r =>
                    (r.Comment != null && r.Comment.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (r.Reviewer?.Name != null && r.Reviewer.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (r.Reviewee?.Name != null && r.Reviewee.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (r.Category?.NameEn != null && r.Category.NameEn.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (r.Category?.NameAr != null && r.Category.NameAr.Contains(search, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            if (page < 1) page = 1;
            int pageSize = 5;
            int currentPage = page;
            double totalPages = Math.Ceiling(reviews.Count() / (double)pageSize);
            reviews = reviews.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new AdminReviewsIndexVM
            {
                Reviews = reviews,
                Search = search,
                CurrentPage = currentPage,
                TotalPages = totalPages
            };

            return View(vm);
        }

        // ───────── CREATE (GET) ─────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var bookings = await _bookingRepo.GetAsync(
                b => b.Status == BookingStatus.Completed,
                includes: [b => b.Client, b => b.Worker.User],
                tracking: false
            );

            var existingReviewBookingIds = (await _reviewRepo.GetAsync(tracking: false))
                .Select(r => r.BookingId)
                .ToHashSet();

            var eligibleBookings = bookings
                .Where(b => !existingReviewBookingIds.Contains(b.Id))
                .ToList();

            var vm = new AdminReviewVM
            {
                Bookings = eligibleBookings.Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = $"Booking #{b.Id} - Client: {b.Client?.Name} -> Worker: {b.Worker?.User?.Name}"
                }).ToList()
            };

            return View(vm);
        }

        // ───────── CREATE (POST) ─────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminReviewVM model)
        {
            if (!ModelState.IsValid)
            {
                var bookings = await _bookingRepo.GetAsync(
                    b => b.Status == BookingStatus.Completed,
                    includes: [b => b.Client, b => b.Worker.User],
                    tracking: false
                );
                var existingReviewBookingIds = (await _reviewRepo.GetAsync(tracking: false))
                    .Select(r => r.BookingId)
                    .ToHashSet();
                var eligibleBookings = bookings
                    .Where(b => !existingReviewBookingIds.Contains(b.Id))
                    .ToList();

                model.Bookings = eligibleBookings.Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = $"Booking #{b.Id} - Client: {b.Client?.Name} -> Worker: {b.Worker?.User?.Name}"
                }).ToList();

                TempData["error-notification"] = "Please correct all form validation errors.";
                return View(model);
            }

            var booking = await _bookingRepo.GetOneAsync(
                b => b.Id == model.BookingId,
                includes: [b => b.Worker, b => b.Worker.WorkerServices],
                tracking: false
            );

            if (booking is null)
            {
                TempData["error-notification"] = "Selected booking not found.";
                return RedirectToAction(nameof(Create));
            }

            var categoryId = booking.Worker?.WorkerServices?.CategoryId ?? 0;
            if (categoryId == 0)
            {
                TempData["error-notification"] = "Selected worker does not have a service category registered.";
                return RedirectToAction(nameof(Create));
            }

            var review = new Review
            {
                BookingId = model.BookingId,
                OrderId = model.BookingId,
                CategoryId = categoryId,
                Direction = ReviewDirection.ClientToWorker,
                Rating = model.Rating,
                Comment = model.Comment.Trim(),
                ReviewerId = booking.ClientId,
                RevieweeId = booking.Worker.UserId,
                IsApproved = true, // Manually created admin reviews are approved by default
                WorkerProfileId = booking.WorkerId,
                CreatedAt = DateTime.UtcNow
            };

            await _reviewRepo.CreateAsync(review);
            await _reviewRepo.CommitAsync();

            // Update average rating
            await RecalculateWorkerRatingAsync(booking.Worker.UserId);

            TempData["success-notification"] = "Feedback created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ───────── EDIT (GET) ─────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var review = await _reviewRepo.GetOneAsync(r => r.Id == id, includes: [r => r.Reviewer, r => r.Reviewee], tracking: false);
            if (review is null)
            {
                TempData["error-notification"] = "Feedback not found.";
                return NotFound();
            }

            var vm = new AdminReviewVM
            {
                Id = review.Id,
                BookingId = review.BookingId,
                Rating = review.Rating,
                Comment = review.Comment
            };

            ViewBag.ReviewerName = review.Reviewer?.Name ?? $"{review.Reviewer?.FName} {review.Reviewer?.LName}".Trim();
            if (string.IsNullOrWhiteSpace(ViewBag.ReviewerName)) ViewBag.ReviewerName = review.Reviewer?.UserName ?? "Client";

            ViewBag.RevieweeName = review.Reviewee?.Name ?? $"{review.Reviewee?.FName} {review.Reviewee?.LName}".Trim();
            if (string.IsNullOrWhiteSpace(ViewBag.RevieweeName)) ViewBag.RevieweeName = review.Reviewee?.UserName ?? "Worker";

            return View(vm);
        }

        // ───────── EDIT (POST) ─────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminReviewVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = "Please correct all form validation errors.";
                return View(model);
            }

            var review = await _reviewRepo.GetOneAsync(r => r.Id == model.Id, tracking: true);
            if (review is null)
            {
                TempData["error-notification"] = "Feedback not found.";
                return NotFound();
            }

            review.Rating = model.Rating;
            review.Comment = model.Comment.Trim();

            await _reviewRepo.CommitAsync();

            // Recalculate average rating
            await RecalculateWorkerRatingAsync(review.RevieweeId);

            TempData["success-notification"] = "Feedback updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ───────── APPROVE (POST) ─────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _reviewRepo.GetOneAsync(r => r.Id == id, tracking: true);
            if (review is null)
            {
                TempData["error-notification"] = "Feedback not found.";
                return NotFound();
            }

            review.IsApproved = true;

            // Auto-heal: Resolve profile ID if null or zero
            if (review.WorkerProfileId == null || review.WorkerProfileId == 0)
            {
                var profile = await _workerProfileRepo.GetOneAsync(wp => wp.UserId == review.RevieweeId, tracking: false);
                if (profile != null)
                {
                    review.WorkerProfileId = profile.Id;
                }
            }

            await _reviewRepo.CommitAsync();

            // Recalculate average rating
            await RecalculateWorkerRatingAsync(review.RevieweeId);

            TempData["success-notification"] = "Feedback approved successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ───────── DELETE ─────────
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _reviewRepo.GetOneAsync(r => r.Id == id, tracking: true);
            if (review is null)
            {
                return NotFound("Feedback not found.");
            }

            var revieweeId = review.RevieweeId;

            _reviewRepo.Delete(review);
            await _reviewRepo.CommitAsync();

            // Recalculate average rating
            await RecalculateWorkerRatingAsync(revieweeId);

            return Ok();
        }

        // ───────── RECALCULATE RATING HELPER ─────────
        private async Task RecalculateWorkerRatingAsync(string workerUserId)
        {
            // Get all approved reviews for this worker where direction is ClientToWorker
            var workerReviews = await _reviewRepo.GetAsync(
                r => r.RevieweeId == workerUserId && r.IsApproved && r.Direction == ReviewDirection.ClientToWorker,
                tracking: false
            );

            // Fetch the worker's profile
            var profile = await _workerProfileRepo.GetOneAsync(wp => wp.UserId == workerUserId, tracking: true);
            if (profile != null)
            {
                profile.RatingCount = workerReviews.Count();
                profile.RatingAvg = workerReviews.Any()
                    ? (decimal)workerReviews.Average(r => r.Rating)
                    : 0m;

                _workerProfileRepo.Update(profile);
                await _workerProfileRepo.CommitAsync();
            }
        }
    }
}
