using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Shatbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN} , {SD.ROLE_SUPER_ADMIN}")]

    public class HomeController : Controller
    {
        private readonly IRepository<ServiceCategory> _serviceCategoryRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<User> _userRepository;

        public HomeController(IRepository<ServiceCategory> serviceCategoryRepository, IRepository<Order> orderRepository, IRepository<User> userManager)
        {
            _serviceCategoryRepository = serviceCategoryRepository;
            _orderRepository = orderRepository;
            _userRepository = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var usersCount = (await _userRepository.GetAsync()).Count();
            var serviceCategoryCount = (await _serviceCategoryRepository.GetAsync()).Where(x => x.IsActive).Count();
            var orderCount = (await _orderRepository.GetAsync()).Count();
            return View(new DashboardStatsCardCountVM
            {
                ServicesCategoriesCount = serviceCategoryCount,
                OrdersCount = orderCount,
                UsersCount = usersCount
            });
        }
    }
}
