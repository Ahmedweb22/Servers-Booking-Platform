//using Shtbly.Services.Notification;

//namespace Shtbly.Services.Hangfire
//{
//    public class CancelUnconfirmedOrderJob
//    {
//        private readonly IUnitOfWork _unitOfWork;
//        private readonly INotificationService _notificationService;

//        public CancelUnconfirmedOrderJob(
//            IUnitOfWork unitOfWork,
//            INotificationService notificationService)
//        {
//            _unitOfWork = unitOfWork;
//            _notificationService = notificationService;
//        }

//        public async Task ExecuteAsync(int orderId)
//        {
//            var order = await _unitOfWork.Orders.GetOneAsync(o => o.Id == orderId);

//            if (order is null || order.Status != OrderStatuses.Pending)
//                return; // العامل اتصرف بالفعل (أكد/رفض) أو اتلغى، متعملش حاجة

//            order.Status = OrderStatuses.NoResponse;
//            order.CancelledAt = DateTime.UtcNow;
//            order.CancellationReason = "لم يستجب العامل خلال المدة المحددة";

//            _unitOfWork.Orders.Update(order);
//            await _unitOfWork.CommitAsync();

//            // إشعار الكلاينت
//            await _notificationService.CreateNotificationAsync(
//                userId: order.UserId,
//                title: "لم يتم تأكيد الطلب",
//                message: "لم يستجب العامل لطلبك خلال المدة المحددة، يمكنك اختيار عامل آخر",
//                type: NotificationType.Order, // غيّرها حسب الـ enum عندك
//                bookingId: order.BookingId);
//        }
//    }
//}
