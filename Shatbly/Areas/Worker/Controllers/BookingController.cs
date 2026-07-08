using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Shatbly.Models;
using Shatbly.Repositories.IRepositories;
using Shatbly.Services.Notification;
using Microsoft.AspNetCore.Localization;
using Shatbly.Services.BookingSystem;

using Shatbly.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Shatbly.Areas.Worker.Controllers
{
    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_WORKER}")]
    public class BookingController : Controller
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly INotificationService _notificationService;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public BookingController(
            IRepository<Order> orderRepository,
            INotificationService notificationService,
            UserManager<User> userManager,
            ApplicationDbContext context)
        {
            _orderRepository = orderRepository;
            _notificationService = notificationService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            var orders = await _orderRepository.GetAsync(
                expression: o => o.WorkerId == currentUserId,
                includes: new System.Linq.Expressions.Expression<System.Func<Order, object>>[] 
                { 
                    o => o.Service, 
                    o => o.User,
                    o => o.Booking
                },
                tracking: false
            );

            var orderedList = orders.OrderByDescending(o => o.CreatedAt).ToList();
            return View(orderedList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatuses status)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var order = await _orderRepository.GetOneAsync(
                expression: o => o.Id == orderId,
                includes: new System.Linq.Expressions.Expression<System.Func<Order, object>>[] 
                { 
                    o => o.Booking, 
                    o => o.Service, 
                    o => o.User 
                }
            );

            if (order == null || order.WorkerId != currentUserId)
            {
                return NotFound();
            }

            // Perform status transition
            order.Status = status;

            // Sync with parent Booking if present
            if (order.Booking != null)
            {
                if (status == OrderStatuses.Confirmed)
                {
                    order.Booking.Status = BookingStatus.Confirmed;
                }
                else if (status == OrderStatuses.Completed)
                {
                    order.Booking.Status = BookingStatus.Completed;

                    // Fetch the worker's wallet
                    var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == order.WorkerId);
                    if (wallet == null)
                    {
                        wallet = new Wallet { UserId = order.WorkerId, Balance = 0, UpdatedAt = DateTime.UtcNow };
                        await _context.Wallets.AddAsync(wallet);
                        await _context.SaveChangesAsync();
                    }

                    // Calculate payout: TotalPrice - ConvenienceFee
                    decimal payout = Math.Max(0m, order.TotalPrice - order.ConvenienceFee);

                    // Credit wallet
                    wallet.Balance += payout;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    _context.Wallets.Update(wallet);

                    // Create WalletTransaction
                    var transaction = new WalletTransaction
                    {
                        WalletId = wallet.Id,
                        Amount = payout,
                        Type = WalletTransactionType.Earning,
                        Reference = $"Payout for Booking #{order.Id}",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.WalletTransactions.AddAsync(transaction);
                    await _context.SaveChangesAsync();
                }
                else if (status == OrderStatuses.Cancelled || status == OrderStatuses.Rejected)
                {
                    order.Booking.Status = BookingStatus.Cancelled;
                }
            }

            _orderRepository.Update(order);
            await _orderRepository.CommitAsync();

            // Notify the customer
            string title = "";
            string message = "";
            var reqCultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            var isAr = reqCultureFeature?.RequestCulture?.UICulture?.Name.StartsWith("ar") ?? false;

            var workerName = User.Identity?.Name ?? (isAr ? "الفني" : "Worker");

            if (status == OrderStatuses.Confirmed)
            {
                title = isAr ? "تم قبول طلبك" : "Booking Confirmed";
                message = isAr 
                    ? $"تم قبول طلب الحجز للخدمة ({order.Service?.NameAr ?? order.Service?.NameEn}) من قبل الفني {workerName}." 
                    : $"Your booking request for {order.Service?.NameEn} has been accepted by the worker {workerName}.";
                TempData["Success"] = isAr ? "تم قبول الطلب وتأكيده بنجاح." : "Order confirmed successfully.";
            }
            else if (status == OrderStatuses.Rejected)
            {
                title = isAr ? "تم رفض طلبك" : "Booking Rejected";
                message = isAr 
                    ? $"نأسف، تم رفض طلب حجز الخدمة ({order.Service?.NameAr ?? order.Service?.NameEn}) من قبل الفني {workerName}." 
                    : $"Your booking request for {order.Service?.NameEn} has been rejected by the worker {workerName}.";
                TempData["Success"] = isAr ? "تم رفض الطلب وإلغائه." : "Order has been rejected.";
            }
            else if (status == OrderStatuses.Completed)
            {
                title = isAr ? "اكتملت الخدمة" : "Service Completed";
                message = isAr 
                    ? $"تم إكمال الخدمة ({order.Service?.NameAr ?? order.Service?.NameEn}) من قبل الفني {workerName}." 
                    : $"The worker {workerName} has completed the service {order.Service?.NameEn}.";
                TempData["Success"] = isAr ? "تم تحديد الطلب كمكتمل بنجاح." : "Order marked as completed.";
            }

            if (!string.IsNullOrEmpty(title))
            {
                await _notificationService.CreateNotificationAsync(
                    order.UserId,
                    title,
                    message,
                    NotificationType.BookingUpdate,
                    order.BookingId
                );
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> RaiseDispute(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            var order = await _orderRepository.GetOneAsync(
                expression: o => o.Id == id,
                includes: new System.Linq.Expressions.Expression<System.Func<Order, object>>[] 
                { 
                    o => o.Booking, 
                    o => o.Service, 
                    o => o.User 
                }
            );

            var reqCultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            var isAr = reqCultureFeature?.RequestCulture?.UICulture?.Name.StartsWith("ar") ?? false;

            if (order == null || order.WorkerId != currentUserId)
            {
                TempData["Error"] = isAr ? "الطلب غير موجود أو لا تملك صلاحية الوصول إليه." : "Booking not found or you don't have access.";
                return RedirectToAction(nameof(Index));
            }

            if (order.Booking?.Status == BookingStatus.Disputed)
            {
                TempData["Error"] = isAr ? "تم تقديم نزاع بالفعل لهذا الطلب." : "This booking is already disputed.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CustomerName = order.User?.Name ?? (isAr ? "عميل غير معروف" : "Unknown Client");
            ViewBag.ServiceName = isAr ? (order.Service?.NameAr ?? order.Service?.NameEn ?? "") : (order.Service?.NameEn ?? "");
            ViewBag.OrderId = id;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RaiseDispute(int id, string reason, [FromServices] IBookingSystemService bookingSystemService)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var order = await _orderRepository.GetOneAsync(
                expression: o => o.Id == id,
                includes: new System.Linq.Expressions.Expression<System.Func<Order, object>>[] 
                { 
                    o => o.Booking, 
                    o => o.Service, 
                    o => o.User 
                }
            );

            var reqCultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            var isAr = reqCultureFeature?.RequestCulture?.UICulture?.Name.StartsWith("ar") ?? false;

            if (order == null || order.WorkerId != currentUserId)
            {
                TempData["Error"] = isAr ? "الطلب غير موجود أو لا تملك صلاحية الوصول إليه." : "Booking not found or you don't have access.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(reason))
            {
                TempData["Error"] = isAr ? "سبب النزاع مطلوب." : "Dispute reason is required.";
                return RedirectToAction(nameof(RaiseDispute), new { id });
            }

            var result = await bookingSystemService.RaiseDisputeAsync(id, reason, currentUserId);
            if (result)
            {
                TempData["Success"] = isAr ? "تم رفع النزاع بنجاح." : "Dispute raised successfully.";
            }
            else
            {
                TempData["Error"] = isAr ? "فشل رفع النزاع للطلب." : "Failed to raise dispute.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
