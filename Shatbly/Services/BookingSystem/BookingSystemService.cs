using Stripe;
using BookingTypes = Shatbly.ViewModels.BookingTypes;

namespace Shatbly.Services.BookingSystem;
public enum RecurrencePatterns
{
    None,
    Weekly,
    Monthly
}
public enum PaymentMethods
{
    None,
    Cash,
    Vesa,
    Deposited,
    Paypal,
    Card,
    Wallet
}

public class BookingSystemService : IBookingSystemService
{
    private const int WorkerResponseWindowMinutes = 30;
    private static readonly TimeSpan SlotLeadTime = TimeSpan.FromHours(1);

    private readonly IRepository<ServiceCategory> _serviceCategoryRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly UserManager<User> _userManager;

    public BookingSystemService(
        UserManager<User> userManager,
        IRepository<ServiceCategory> serviceCategoryRepository,
        IRepository<Order> orderRepository)
    {
        _userManager = userManager;
        _serviceCategoryRepository = serviceCategoryRepository;
        _orderRepository = orderRepository;
    }

    public async Task<BookingWizardViewModel> BuildCreateViewModelAsync(BookingWizardViewModel? model = null)
    {
        model ??= new BookingWizardViewModel();

        var services = (await _serviceCategoryRepository.GetAsync(s => s.IsActive))
            .OrderBy(s => s.Id)
            .ToList();

        var workers = (await _userManager.GetUsersInRoleAsync(SD.ROLE_WORKER))
        .OrderBy(w => w.Name)
        .ToList();


        var selectedService = services.FirstOrDefault(s => s.Id == model.ServiceId) ?? services.FirstOrDefault();
        var selectedWorker = workers.FirstOrDefault(w => w.Id == model.WorkerId) ?? workers.FirstOrDefault();

        if (selectedService is not null && model.ServiceId is null)
        {
            model.ServiceId = selectedService.Id;
        }

        if (selectedWorker is not null && model.WorkerId is null)
        {
            model.WorkerId = selectedWorker.Id;
        }

        ApplyDefaultCustomerValues(model);

        var pricing = CalculatePricing(selectedService?.Price ?? 0, model.BookingType, (RecurrencePatterns)model.RecurrencePattern);

        model.ServiceOptions = services
            .Select(s => new SelectListItem($"{s.Name} - EGP {s.Price:0.00}", s.Id.ToString()))
            .ToList();

        model.WorkerOptions = workers
            .Select(w => new SelectListItem(w.Name, w.Id.ToString()))
            .ToList();
        model.PaymentOptions = new List<SelectListItem>
{
    new SelectListItem("Cash", PaymentMethods.Cash.ToString()),
    new SelectListItem("Card", PaymentMethods.Card.ToString()),
    new SelectListItem("Wallet", PaymentMethods.Wallet.ToString())
};

        model.RecurrenceOptions = new List<SelectListItem>
{
    new SelectListItem("One-time", RecurrencePatterns.None.ToString()),
    new SelectListItem("Weekly", RecurrencePatterns.Weekly.ToString()),
    new SelectListItem("Monthly", RecurrencePatterns.Monthly.ToString())
};

        model.AddressPresets =
        [
            new SelectListItem("Home - 24 Palm Street, New Cairo", "24 Palm Street, New Cairo"),
            new SelectListItem("Office - 8 Corniche Road, Maadi", "8 Corniche Road, Maadi"),
            new SelectListItem("Villa - 15 Lotus Compound, Sheikh Zayed", "15 Lotus Compound, Sheikh Zayed")
        ];

        model.AvailabilityJson = JsonSerializer.Serialize(await BuildAvailabilityAsync(workers.Select(w => w.Id)));
        model.SelectedServiceName = selectedService?.Name ?? "Choose a service";
        model.SelectedWorkerName = selectedWorker?.Name ?? "Choose a worker";

        model.ServicePrice = pricing.ServicePrice;
        model.ConvenienceFee = pricing.ConvenienceFee;
        model.DiscountAmount = pricing.DiscountAmount;
        model.TotalPrice = pricing.TotalPrice;

        if (string.IsNullOrWhiteSpace(model.SelectedDate) &&
            selectedWorker is not null &&
            await TryGetEarliestAvailableSlotAsync(selectedWorker.Id) is DateTime earliest)
        {
            model.SelectedDate = earliest.ToString("yyyy-MM-dd");
            model.SelectedTime = earliest.ToString("HH:mm");
        }

        return model;

    }
    private async Task<Dictionary<string, Dictionary<string, List<string>>>> BuildAvailabilityAsync(IEnumerable<string> workerIds)
    {
        var availability = new Dictionary<string, Dictionary<string, List<string>>>();

        foreach (var workerId in workerIds.Distinct())
        {
            availability[workerId] = await GetAvailableSlotsByDateAsync(workerId);
        }

        return availability;
    }
    private async Task<DateTime?> TryGetEarliestAvailableSlotAsync(string workerId)
    {
        var availability = await GetAvailableSlotsByDateAsync(workerId);
        var firstDate = availability.OrderBy(kvp => kvp.Key).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstDate.Key) || firstDate.Value.Count == 0)
        {
            return null;
        }

        return DateTime.TryParse($"{firstDate.Key} {firstDate.Value[0]}", out var scheduledAt)
            ? scheduledAt
            : null;
    }


    public async Task<BookingCreateResult> CreateAsync(BookingWizardViewModel model)
    {
        var validationErrors = new Dictionary<string, IReadOnlyList<string>>();

        var service = (await _serviceCategoryRepository.GetAsync(s => s.Id == model.ServiceId && s.IsActive))
            .FirstOrDefault();

        var worker = model.WorkerId is null
     ? null
     : await _userManager.FindByIdAsync(model.WorkerId);

        // «· ⁄œÌ·: œ„Ã ‘—Êÿ «· Õﬁﬁ „‰ «·⁄«„· · Ã‰»  ﬂ—«— Ê œ«Œ· «·√Œÿ«¡
        if (worker is null)
        {
            validationErrors[nameof(model.WorkerId)] = ["Choose a worker."];
        }
        else if (!await _userManager.IsInRoleAsync(worker, SD.ROLE_WORKER))
        {
            validationErrors[nameof(model.WorkerId)] = ["The selected worker is not available."];
        }

        if (service is null)
        {
            validationErrors[nameof(model.ServiceId)] = ["The selected service is not available."];
        }

        var resolvedScheduledAt = service is not null && worker is not null
            ? await TryResolveScheduledAtAsync(model, worker.Id, null)
            : null;

        if (resolvedScheduledAt is not DateTime)
        {
            validationErrors[nameof(model.SelectedTime)] = ["Choose an available date and time."];
        }

        if (validationErrors.Count > 0)
        {
            return new BookingCreateResult
            {
                Succeeded = false,
                ViewModel = await BuildCreateViewModelAsync(model),
                ValidationErrors = validationErrors
            };
        }

        var customer = await GetOrCreateCustomerAsync(model);
        var pricing = CalculatePricing(service!.Price, model.BookingType, (RecurrencePatterns)model.RecurrencePattern);

        var order = new Order
        {
            UserId = customer.Id,
            ServiceId = service.Id,
            WorkerId = worker!.Id,
            Status = OrderStatuses.Pending,
            BookingType = (Shatbly.Models.BookingTypes)Enum.Parse(
                typeof(Shatbly.Models.BookingTypes),
                model.BookingType.ToString()),
            ScheduledAt = resolvedScheduledAt!.Value,
            DurationHours = 1,
            AddressLabel = model.AddressLabel,
            AddressLine = model.AddressLine,
            Notes = model.Notes,
            PaymentMethod = model.PaymentMethod,
            PaymentStatus = model.PaymentMethod is (Models.PaymentMethods)PaymentMethods.Cash
                ? PaymentStatuses.Pending
                : PaymentStatuses.Paid,
            RecurrencePattern = model.RecurrencePattern,
            ServicePrice = pricing.ServicePrice,
            ConvenienceFee = pricing.ConvenienceFee,
            DiscountAmount = pricing.DiscountAmount,
            TotalPrice = pricing.TotalPrice,
            WorkerResponseDeadlineUtc = DateTime.UtcNow.AddMinutes(WorkerResponseWindowMinutes),
            CreatedAt = DateTime.UtcNow
        };

        // «· ⁄œÌ·:  ›⁄Ì· ”ÿ— ≈÷«›… «·ÿ·» ≈·Ï «·„” Êœ⁄
        await _orderRepository.CreateAsync(order);
        await _orderRepository.CommitAsync();

        return new BookingCreateResult
        {
            Succeeded = true,
            BookingId = order.Id,
            SuccessMessage = "Booking created. The assigned worker has 30 minutes to accept or reject it."
        };
    }
    public async Task<BookingDetailsViewModel?> GetDetailsAsync(int id)
    {
        var booking = (await _orderRepository.GetAsync(o => o.Id == id)).FirstOrDefault();
        if (booking is null)
        {
            return null;
        }

        await PopulateNavigationAsync(booking);

        return new BookingDetailsViewModel
        {
            Booking = booking,
            RefundPreview = CalculateRefund(booking),
            CanCancel = CanManageBooking(booking),
            CanReschedule = CanManageBooking(booking)
        };
    }

    public async Task<BookingActionResult> RescheduleAsync(int id, string scheduledAt)
    {
        var booking = (await _orderRepository.GetAsync(o => o.Id == id)).FirstOrDefault();
        if (booking is null)
        {
            return new BookingActionResult { NotFound = true };
        }

        if (!DateTime.TryParse(scheduledAt, out var parsedSlot) || booking.WorkerId is null)
        {
            return FailAction(id, "Choose a valid slot to reschedule.");
        }

        var slot = await TryResolveScheduledAtAsync(
            new BookingWizardViewModel
            {
                BookingType = BookingTypes.Scheduled,
                SelectedDate = parsedSlot.ToString("yyyy-MM-dd"),
                SelectedTime = parsedSlot.ToString("HH:mm")
            },
            booking.WorkerId,
            booking.Id);

        if (slot is not DateTime validSlot)
        {
            return FailAction(id, "That slot is no longer available.");
        }

        booking.ScheduledAt = validSlot;
        booking.Status = OrderStatuses.Rescheduled;
        booking.WorkerResponseDeadlineUtc = DateTime.UtcNow.AddMinutes(WorkerResponseWindowMinutes);

         _orderRepository.Update(booking);
        await _orderRepository.CommitAsync();
        //await _userManager.UpdateAsync(booking);


        return new BookingActionResult
        {
            Succeeded = true,
            BookingId = id,
            Message = "Booking rescheduled. Worker confirmation has been requested again."
        };
    }

    public async Task<BookingActionResult> CancelAsync(int id, string? cancellationReason)
    {
        var booking = (await _orderRepository.GetAsync(o => o.Id == id)).FirstOrDefault();
        if (booking is null)
        {
            return new BookingActionResult { NotFound = true };
        }

        booking.Status = OrderStatuses.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancellationReason = string.IsNullOrWhiteSpace(cancellationReason)
            ? "Cancelled by customer"
            : cancellationReason.Trim();
        booking.RefundAmount = CalculateRefund(booking);

        _orderRepository.Update(booking);
        await _orderRepository.CommitAsync();
        //await _userManager.UpdateAsync(booking);


        return new BookingActionResult
        {
            Succeeded = true,
            BookingId = id,
            Message = $"Booking cancelled. Estimated refund: EGP {booking.RefundAmount:0.00}."
        };
    }

    private async Task PopulateNavigationAsync(Order booking)
    {
        booking.Service = (await _serviceCategoryRepository.GetAsync(s => s.Id == booking.ServiceId))
            .FirstOrDefault();

        booking.User = await _userManager.FindByIdAsync(booking.UserId.ToString());

        if (!string.IsNullOrWhiteSpace(booking.WorkerId))
        {
            booking.Worker = await _userManager.FindByIdAsync(booking.WorkerId);
        }
    }

    private static void ApplyDefaultCustomerValues(BookingWizardViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.CustomerName))
        {
            return;
        }

        model.CustomerName = "Sara Ahmed";
        model.CustomerEmail = "sara@example.com";
        model.CustomerPhone = "0100000001";
        model.AddressLine = "24 Palm Street, New Cairo";
    }

    private async Task<User> GetOrCreateCustomerAsync(BookingWizardViewModel model)
    {
        var customer = await _userManager.FindByEmailAsync(model.CustomerEmail);

        if (customer is null)
        {
            customer = new User
            {
                UserName = model.CustomerEmail,
                Name = model.CustomerName,
                Email = model.CustomerEmail,
                PhoneNumber = model.CustomerPhone
             };

            var createResult = await _userManager.CreateAsync(customer);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Customer creation failed: {errors}");
            }

            await _userManager.AddToRoleAsync(customer, SD.ROLE_CUSTOMER);
            return customer;
        }

        customer.Name = model.CustomerName;
        customer.PhoneNumber = model.CustomerPhone;
        await _userManager.UpdateAsync(customer);

        if (!await _userManager.IsInRoleAsync(customer, SD.ROLE_CUSTOMER))
        {
            await _userManager.AddToRoleAsync(customer, SD.ROLE_CUSTOMER);
        }

        return customer;
    }

    private static (decimal ServicePrice, decimal ConvenienceFee, decimal DiscountAmount, decimal TotalPrice)
    CalculatePricing(decimal servicePrice, BookingTypes bookingType, RecurrencePatterns recurrencePattern)
{
    var convenienceFee = bookingType == BookingTypes.Instant ? 45m : 25m;

    var discount = recurrencePattern switch
    {
        RecurrencePatterns.Weekly => Math.Round(servicePrice * 0.05m, 2),
        RecurrencePatterns.Monthly => Math.Round(servicePrice * 0.10m, 2),
        _ => 0m
    };

    return (
        ServicePrice: servicePrice,
        ConvenienceFee: convenienceFee,
        DiscountAmount: discount,
        TotalPrice: servicePrice + convenienceFee - discount
    );
}



    private async Task<Dictionary<string, List<string>>> GetAvailableSlotsByDateAsync(string workerId, int? ignoreOrderId = null)
    {
        var result = new Dictionary<string, List<string>>();
        var now = DateTime.Now;

        var activeStatuses = new[]
        {
        OrderStatuses.Pending,
        OrderStatuses.Confirmed,
        OrderStatuses.Rescheduled,
        OrderStatuses.Completed
    };

        var workerBookings = (await _orderRepository.GetAsync(o =>
                o.WorkerId == workerId &&
                (ignoreOrderId == null || o.Id != ignoreOrderId) &&
                activeStatuses.Contains(o.Status) &&
                o.ScheduledAt >= now.Date))
            .Select(o => o.ScheduledAt)
            .ToList();

        for (var dayOffset = 0; dayOffset < 10; dayOffset++)
        {
            var date = now.Date.AddDays(dayOffset);
            var slots = new List<string>();

            for (var hour = 9; hour <= 18; hour++)
            {
                var slot = date.AddHours(hour);

                if (slot <= now.Add(SlotLeadTime))
                {
                    continue;
                }

                if (workerBookings.Any(existing => existing == slot))
                {
                    continue;
                }

                slots.Add(slot.ToString("HH:mm"));
            }

            if (slots.Count > 0)
            {
                result[date.ToString("yyyy-MM-dd")] = slots;
            }
        }

        return result;
    }

    private async Task<DateTime?> TryResolveScheduledAtAsync(BookingWizardViewModel model, string workerId, int? ignoreOrderId)
    {
        if (model.BookingType == BookingTypes.Instant)
        {
            return await TryGetEarliestAvailableSlotAsync(workerId);
        }

        if (!DateTime.TryParse($"{model.SelectedDate} {model.SelectedTime}", out var scheduledAt))
        {
            return null;
        }

        return await IsSlotAvailableAsync(workerId, scheduledAt, ignoreOrderId)
            ? scheduledAt
            : null;
    }


    private async Task<bool> IsSlotAvailableAsync(string workerId, DateTime scheduledAt, int? ignoreOrderId = null)
    {
        var availability = await GetAvailableSlotsByDateAsync(workerId, ignoreOrderId);

        return availability.TryGetValue(scheduledAt.ToString("yyyy-MM-dd"), out var times)
               && times.Contains(scheduledAt.ToString("HH:mm"));
    }

    private static bool CanManageBooking(Order booking)
    {
        return booking.Status is OrderStatuses.Pending or OrderStatuses.Confirmed or OrderStatuses.Rescheduled;
    }

    private static decimal CalculateRefund(Order booking)
    {
        var hoursUntilBooking = (booking.ScheduledAt - DateTime.Now).TotalHours;

        if (hoursUntilBooking >= 24)
        {
            return booking.TotalPrice;
        }

        if (hoursUntilBooking >= 2)
        {
            return Math.Round(booking.TotalPrice * 0.5m, 2);
        }

        return 0m;
    }

    private static BookingActionResult FailAction(int bookingId, string message)
    {
        return new BookingActionResult
        {
            BookingId = bookingId,
            Message = message
        };
    }
}
