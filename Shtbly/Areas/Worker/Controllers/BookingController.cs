using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Shtbly.Models;
using Shtbly.Repositories.IRepositories;
using Shtbly.Services.Notification;
using Microsoft.AspNetCore.Localization;
using Shtbly.Services.BookingSystem;

using Shtbly.DataAccess;
using Microsoft.EntityFrameworkCore;
using Shtbly.UnitOfWork;
using Shtbly.Services.Receipt;
using Shtbly.Utilities;

namespace Shtbly.Areas.Worker.Controllers
{
    [Area(SD.WORKER_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN},{SD.ROLE_WORKER}")]
    public class BookingController : Controller
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly INotificationService _notificationService;
        private readonly UserManager<User> _userManager;
        private readonly IBookingSystemService _bookingSystemService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReceiptService _receiptService;
        private readonly IEmailSenderWithAttachment _emailSender;

        public BookingController(
            IRepository<Order> orderRepository,
            INotificationService notificationService,
            UserManager<User> userManager,
            IBookingSystemService bookingSystemService,
            IUnitOfWork unitOfWork,
            IReceiptService receiptService,
            IEmailSenderWithAttachment emailSender)
        {
            _orderRepository = orderRepository;
            _notificationService = notificationService;
            _userManager = userManager;
            _bookingSystemService = bookingSystemService;
            _unitOfWork = unitOfWork;
            _receiptService = receiptService;
            _emailSender = emailSender;
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
                    o => o.Booking,
                    o => o.Booking.Payment
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

            var isAr = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.Name == "ar";
            var workerName = isAr ? User.Identity.Name : User.Identity.Name;

            bool success = await _bookingSystemService.UpdateWorkerOrderStatusAsync(orderId, status, currentUserId, isAr, workerName);

            if (!success)
            {
                return NotFound();
            }

            var order = await _orderRepository.GetOneAsync(
                expression: o => o.Id == orderId,
                includes: new System.Linq.Expressions.Expression<System.Func<Order, object>>[] { o => o.Service! },
                tracking: false);

            if (order == null)
            {
                return NotFound();
            }

            // Notify the customer
            string title = "";
            string message = "";

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int orderId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var order = await _orderRepository.GetOneAsync(
                expression: o => o.Id == orderId,
                includes: new System.Linq.Expressions.Expression<System.Func<Order, object>>[] { 
                    o => o.Booking,
                    o => o.Booking.Payment
                }
            );

            if (order == null || order.WorkerId != currentUserId)
            {
                TempData["Error"] = "Booking not found or you don't have access.";
                return RedirectToAction(nameof(Index));
            }

            if (order.PaymentMethod == Shtbly.Models.PaymentMethods.Cash && order.PaymentStatus == Shtbly.Models.PaymentStatuses.Pending)
            {
                order.PaymentStatus = Shtbly.Models.PaymentStatuses.Paid;
                
                if (order.Booking != null)
                {
                    // Also update the underlying booking's payment status if present
                    var booking = order.Booking;
                    if (booking.Payment != null)
                    {
                        booking.Payment.Status = Shtbly.Models.PaymentStatus.Paid;
                        booking.Payment.PaidAt = DateTime.UtcNow;
                    }
                    else
                    {
                        booking.Payment = new Shtbly.Models.Payment
                        {
                            BookingId = booking.Id,
                            Amount = booking.TotalPrice,
                            Method = Shtbly.Models.PaymentMethod.Cash,
                            Status = Shtbly.Models.PaymentStatus.Paid,
                            GatewayName = "Cash",
                            GatewayRef = "CASH-REF",
                            GatewayResponse = "Paid to worker directly",
                            TransactionId = "CASH-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                            PaidAt = DateTime.UtcNow
                        };
                    }
                }
                
                await _orderRepository.CommitAsync();

                // Generate Receipt and Send Emails
                if (order.Booking != null)
                {
                    // Reload booking with full includes to ensure we have Client and Worker.User for emails
                    var fullBooking = await _unitOfWork.Bookings.GetOneAsync(
                        b => b.Id == order.BookingId, 
                        new System.Linq.Expressions.Expression<System.Func<Shtbly.Models.Booking, object>>[] { 
                            b => b.Client, 
                            b => b.Worker.User, 
                            b => b.Payment 
                        });

                    if (fullBooking != null)
                    {
                        var receiptPath = await _receiptService.GenerateReceiptPdfAsync(fullBooking);

                        await _notificationService.CreateNotificationAsync(
                            fullBooking.ClientId,
                            "Payment Receipt",
                            $"Your cash payment for booking #{fullBooking.Id} was recorded.",
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
                                $"<p>A cash payment was recorded for booking #{fullBooking.Id}. Please find the receipt attached.</p>",
                                receiptPath);
                        }
                    }
                }

                TempData["Success"] = "Payment marked as received.";
            }
            else
            {
                TempData["Error"] = "This booking cannot be marked as paid.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
