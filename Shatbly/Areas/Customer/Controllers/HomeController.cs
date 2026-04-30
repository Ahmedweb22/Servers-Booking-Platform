using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Models;
using Shatbly.Services.BookingSystem;

namespace Shatbly.Controllers
{
    [Area(SD.CUSTOMER_AREA)]
    public class HomeController : Controller
    {
        private readonly IBookingSystemService _bookingSystemService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IBookingSystemService bookingSystemService)
        {
            _logger = logger;
            _bookingSystemService = bookingSystemService;
        }

        public IActionResult Index()
        {
            return View();
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
