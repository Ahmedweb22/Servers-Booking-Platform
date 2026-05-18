using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Models;
using Shatbly.Services.BookingSystem;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Linq.Expressions;
namespace Shatbly.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    [Authorize(Roles =$"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_CUSTOMER}")]
    public class HomeController : Controller
    {
        private readonly IBookingSystemService _bookingSystemService;
        private readonly UserManager<User> _userManager;
        private readonly IRepository<Favorite> _favoriteRepository;
        private readonly IRepository<WorkerProfile> _workerRepository;
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IRepository<WorkerService> _serviceRepository;
        private readonly IRepository<ServiceCategory> _categoryRepository;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IBookingSystemService bookingSystemService, UserManager<User> userManager, IRepository<Favorite> favoriteRepository, IRepository<WorkerProfile> workerRepository, IRepository<Booking> bookingRepository, IRepository<WorkerService> serviceRepository, IRepository<ServiceCategory> categoryRepository)
        {
            _logger = logger;
            _bookingSystemService = bookingSystemService;
            _userManager = userManager;
            _favoriteRepository = favoriteRepository;
            _workerRepository = workerRepository;
            _bookingRepository = bookingRepository;
            _serviceRepository = serviceRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index(int? categoryId, string searchString, string city, string district)
        {
            Expression<Func<WorkerProfile, bool>> filter = null;
      
            // Build filter based on category and search
            if(categoryId.HasValue && categoryId > 0)
            {
                if (!string.IsNullOrEmpty(searchString))
                {
                    filter = w => w.WorkerServices.CategoryId == categoryId && w.User.FName.Contains(searchString);
                }
                else
                {
                    filter = w => w.WorkerServices.CategoryId == categoryId;
                }   
            }
            else if (!string.IsNullOrEmpty(searchString))
            {
                filter = w => w.User.FName.Contains(searchString);
            }

            var workers = await _workerRepository.GetAsync(expression: filter, includes: [w => w.WorkerServices.Category, w => w.User.Addresses]);

            // Apply location filter (city/district) after fetching
            if (!string.IsNullOrEmpty(city))
            {
                workers = workers.Where(w => w.User?.Addresses != null && w.User.Addresses.Any(a => a.City == city)).ToList();
                
                if (!string.IsNullOrEmpty(district))
                {
                    workers = workers.Where(w => w.User.Addresses.Any(a => a.District == district)).ToList();
                }
            }

            var categories = await _categoryRepository.GetAsync();
            ViewData["SearchString"] = searchString;
            ViewData["SelectedCategory"] = categoryId?.ToString();
            ViewData["SelectedCity"] = city;
            ViewData["SelectedDistrict"] = district;
            var favoriteWorkerIds = new List<int>();

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var favorites = await _favoriteRepository.GetAsync(f => f.ClientId == userId);
                favoriteWorkerIds = favorites.Select(f => f.WorkerId).ToList();
            }

            var vm = new CustomerIndexVM
            {
                Workers = workers,
                Categories = categories,
                FavoriteWorkerIds = favoriteWorkerIds
            };

            return View(vm);
        }

        public async Task<IActionResult> WorkerDetails(int id)
        {
            var worker = await _workerRepository.GetOneAsync(
                w => w.Id == id,
                includes: [
                    w => w.User.Addresses,
                    w => w.WorkerServices.Category,
                    w => w.WorkerReviews,
                    w => w.Availabilities,
                    w => w.PortfolioMediaItems
                ]);

            if (worker == null)
            {
                return NotFound();
            }

            // Check if this worker is in the current user's favorites
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isFavorite = false;
            if (!string.IsNullOrEmpty(userId))
            {
                var fav = await _favoriteRepository.GetOneAsync(f => f.ClientId == userId && f.WorkerId == id);
                isFavorite = fav != null;
            }

            ViewData["IsFavorite"] = isFavorite;
            return View(worker);
        }

        [HttpPost]
          public async Task<IActionResult> ToggleFavorite(int workerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var existingFavorite = await _favoriteRepository.GetOneAsync(f => f.ClientId == userId && f.WorkerId == workerId);
            if (existingFavorite is null)
            {
                var newFavorite = new Favorite
                {
                    ClientId = userId,
                    WorkerId = workerId,
                    CreatedAt = DateTime.UtcNow
                };
                await _favoriteRepository.CreateAsync(newFavorite);
            }
            else
            {
                 _favoriteRepository.Delete(existingFavorite);
            }
            await _favoriteRepository.CommitAsync();
            return RedirectToAction("Index");
        }
      
    [Authorize]
        public async Task<IActionResult> Favorites()
        {
          var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var favorites = await _favoriteRepository.GetAsync(f => f.ClientId == userId, includes: [f => f.Worker.User.Addresses, f => f.Worker.WorkerServices.Category]);
            var favoriteWorkers = favorites.Select(f => f.Worker).ToList();
            return View(favoriteWorkers);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
