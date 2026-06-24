using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Models;
using Shatbly.Repositories.IRepositories;
using Shatbly.Utilities;
using System.Security.Claims;

namespace Shatbly.Areas.Worker.Controllers
{
    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_WORKER}")]
    public class ReviewController : Controller
    {
        private readonly IRepository<Review> _reviewRepo;

        public ReviewController(IRepository<Review> reviewRepo)
        {
            _reviewRepo = reviewRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                return Unauthorized();
            }

            var reviews = await _reviewRepo.GetAsync(
                r => r.RevieweeId == userId && r.IsApproved,
                includes:
                [
                    r => r.Reviewer,
                    r => r.Category
                ],
                tracking: false);

            return View(reviews.OrderByDescending(r => r.CreatedAt).ToList());
        }
    }
}
