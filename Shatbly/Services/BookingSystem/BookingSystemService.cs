using Stripe;
using BookingTypes = Shatbly.ViewModels.BookingTypes;
using Review = Shatbly.Models.Review;
using Shatbly.DataAccess;
using Microsoft.EntityFrameworkCore;

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
    private readonly IRepository<Review> _reviewRepository;
    private readonly IRepository<Shatbly.Models.Booking> _bookingRepository;
    private readonly IRepository<Shatbly.Models.Address> _addressRepository;
    private readonly IRepository<Shatbly.Models.WorkerProfile> _workerProfileRepository;
    private readonly UserManager<User> _userManager;
    private readonly IStringLocalizer<BookingSystemService> _localizer;
    private readonly ApplicationDbContext _context;

    public BookingSystemService(
        UserManager<User> userManager,
        IRepository<ServiceCategory> serviceCategoryRepository,
        IRepository<Order> orderRepository,
        IRepository<Review> reviewRepository,
        IRepository<Shatbly.Models.Booking> bookingRepository,
        IRepository<Shatbly.Models.Address> addressRepository,
        IRepository<Shatbly.Models.WorkerProfile> workerProfileRepository,
        IStringLocalizer<BookingSystemService> localizer,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _serviceCategoryRepository = serviceCategoryRepository;
        _orderRepository = orderRepository;
        _reviewRepository = reviewRepository;
        _bookingRepository = bookingRepository;
        _addressRepository = addressRepository;
        _workerProfileRepository = workerProfileRepository;
        _localizer = localizer;
        _context = context;
    }

    public async Task<BookingWizardViewModel> BuildCreateViewModelAsync(BookingWizardViewModel? model = null)
    {
        model ??= new BookingWizardViewModel();

        var services = (await _serviceCategoryRepository.GetAsync(s => s.IsActive))
            .OrderBy(s => s.Id)
            .ToList();

        var workers = (await _userManager.GetUsersInRoleAsync(SD.ROLE_WORKER))
        .OrderBy(w => w.FName)
        .ThenBy(w => w.LName)
        .ToList();

        // Load all worker profiles and map them to their service categories and hourly rates
        var workerProfiles = await _context.WorkerProfiles
            .AsNoTracking()
            .Include(wp => wp.WorkerServices)
            .Where(wp => wp.IsApproved && wp.WorkerServices != null)
            .ToListAsync();

        var workerMap = workerProfiles.ToDictionary(
            wp => wp.UserId,
            wp => new { categoryId = wp.WorkerServices.CategoryId, hourlyRate = wp.WorkerServices.HourlyRate }
        );

        model.WorkerDetailsJson = System.Text.Json.JsonSerializer.Serialize(workerMap);

        var serviceMap = services.ToDictionary(s => s.Id, s => s.Price);
        model.ServiceDetailsJson = System.Text.Json.JsonSerializer.Serialize(serviceMap);

        var selectedWorker = workers.FirstOrDefault(w => w.Id == model.WorkerId);
        
        // If a worker is selected, they should determine the service category
        if (selectedWorker != null && workerMap.TryGetValue(selectedWorker.Id, out var selectedWorkerDetails))
        {
            model.ServiceId = selectedWorkerDetails.categoryId;
        }

        var selectedService = services.FirstOrDefault(s => s.Id == model.ServiceId) ?? services.FirstOrDefault();
        if (selectedService is not null && model.ServiceId is null)
        {
            model.ServiceId = selectedService.Id;
        }

        // If no worker was pre-selected, default to the first worker who offers the selected service
        if (selectedWorker == null && selectedService != null)
        {
            selectedWorker = workers.FirstOrDefault(w => workerMap.TryGetValue(w.Id, out var details) && details.categoryId == selectedService.Id);
        }

        if (selectedWorker is not null && model.WorkerId is null)
        {
            model.WorkerId = selectedWorker.Id;
        }

        ApplyDefaultCustomerValues(model);

        decimal basePrice = selectedService?.Price ?? 0;
        if (selectedWorker != null && workerMap.TryGetValue(selectedWorker.Id, out var wDetails))
        {
            basePrice = wDetails.hourlyRate;
        }

        var pricing = CalculatePricing(basePrice, model.BookingType, (RecurrencePatterns)model.RecurrencePattern);

        // Apply promo code discount if valid
        if (model.PromoCodeId.HasValue)
        {
            var promoCode = await _context.PromotionCodes
                .Include(pc => pc.Promotion)
                .FirstOrDefaultAsync(pc => pc.Id == model.PromoCodeId.Value);
            if (promoCode != null && promoCode.IsActive && promoCode.Promotion.IsActive)
            {
                decimal promoDiscount = 0;
                var promotion = promoCode.Promotion;
                if (promotion.DiscountType == DiscountType.Percentage)
                {
                    promoDiscount = Math.Round(pricing.ServicePrice * (promotion.DiscountValue / 100m), 2);
                }
                else if (promotion.DiscountType == DiscountType.FixedAmount)
                {
                    promoDiscount = Math.Min(promotion.DiscountValue, pricing.ServicePrice);
                }

                pricing = (
                    ServicePrice: pricing.ServicePrice,
                    ConvenienceFee: pricing.ConvenienceFee,
                    DiscountAmount: pricing.DiscountAmount + promoDiscount,
                    TotalPrice: Math.Max(0, pricing.TotalPrice - promoDiscount)
                );
            }
        }

        model.ServiceOptions = services
            .Select(s => new SelectListItem(s.Name, s.Id.ToString()))
            .ToList();

        model.WorkerOptions = workers
            .Select(w => {
                var displayName = $"{w.FName} {w.LName}".Trim();
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = w.UserName ?? "Worker";
                }
                return new SelectListItem(displayName, w.Id.ToString());
            })
            .ToList();

        model.PaymentOptions = new List<SelectListItem>
        {
            new SelectListItem(_localizer["Cash"].Value, PaymentMethods.Cash.ToString()),
            new SelectListItem(_localizer["Card"].Value, PaymentMethods.Card.ToString()),
            new SelectListItem(_localizer["Wallet"].Value, PaymentMethods.Wallet.ToString())
        };

        model.RecurrenceOptions = new List<SelectListItem>
        {
            new SelectListItem(_localizer["OneTime"].Value, RecurrencePatterns.None.ToString()),
            new SelectListItem(_localizer["Weekly"].Value, RecurrencePatterns.Weekly.ToString()),
            new SelectListItem(_localizer["Monthly"].Value, RecurrencePatterns.Monthly.ToString())
        };

        model.AddressPresets =
        [
            new SelectListItem("Home - 24 Palm Street, New Cairo", "24 Palm Street, New Cairo"),
            new SelectListItem("Office - 8 Corniche Road, Maadi", "8 Corniche Road, Maadi"),
            new SelectListItem("Villa - 15 Lotus Compound, Sheikh Zayed", "15 Lotus Compound, Sheikh Zayed")
        ];

        model.AvailabilityJson = System.Text.Json.JsonSerializer.Serialize(await BuildAvailabilityAsync(workers.Select(w => w.Id)));
        model.SelectedServiceName = selectedService?.Name ?? _localizer["ChooseService"].Value;

        var selectedWorkerName = selectedWorker != null ? $"{selectedWorker.FName} {selectedWorker.LName}".Trim() : "";
        if (string.IsNullOrWhiteSpace(selectedWorkerName))
        {
            selectedWorkerName = selectedWorker?.UserName ?? _localizer["ChooseWorkerLabel"].Value;
        }
        model.SelectedWorkerName = selectedWorkerName;

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

        var workerProfile = worker is null
            ? null
            : (await _workerProfileRepository.GetAsync(
                wp => wp.UserId == worker.Id,
                includes: new System.Linq.Expressions.Expression<System.Func<WorkerProfile, object>>[] { wp => wp.WorkerServices }
               )).FirstOrDefault();

        if (worker is null)
        {
            validationErrors[nameof(model.WorkerId)] = [_localizer["ChooseWorker"].Value];
        }
        else if (!await _userManager.IsInRoleAsync(worker, SD.ROLE_WORKER))
        {
            validationErrors[nameof(model.WorkerId)] = [_localizer["WorkerNotAvailable"].Value];
        }
        else if (workerProfile is null)
        {
            validationErrors[nameof(model.WorkerId)] = ["Worker profile not found."];
        }

        if (service is null)
        {
            validationErrors[nameof(model.ServiceId)] = [_localizer["ServiceNotAvailable"].Value];
        }

        // Validate service category matching selected worker
        if (workerProfile != null && service != null)
        {
            if (workerProfile.WorkerServices == null || workerProfile.WorkerServices.CategoryId != service.Id)
            {
                validationErrors[nameof(model.WorkerId)] = ["The selected worker does not offer this service."];
            }
        }

        var resolvedScheduledAt = service is not null && worker is not null
            ? await TryResolveScheduledAtAsync(model, worker.Id, null)
            : null;

        if (resolvedScheduledAt is not DateTime)
        {
            validationErrors[nameof(model.SelectedTime)] = [_localizer["ChooseDateTime"].Value];
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

        decimal basePrice = workerProfile!.WorkerServices.HourlyRate;
        var pricing = CalculatePricing(basePrice, model.BookingType, (RecurrencePatterns)model.RecurrencePattern);

        // Validate and apply promo code discount
        Shatbly.Models.PromotionCode? promoCode = null;
        if (model.PromoCodeId.HasValue)
        {
            promoCode = await _context.PromotionCodes
                .Include(pc => pc.Promotion)
                .FirstOrDefaultAsync(pc => pc.Id == model.PromoCodeId.Value);
            if (promoCode != null && promoCode.IsActive && promoCode.Promotion.IsActive && promoCode.UsedCount < promoCode.MaxUses)
            {
                decimal promoDiscount = 0;
                var promotion = promoCode.Promotion;
                if (promotion.DiscountType == DiscountType.Percentage)
                {
                    promoDiscount = Math.Round(pricing.ServicePrice * (promotion.DiscountValue / 100m), 2);
                }
                else if (promotion.DiscountType == DiscountType.FixedAmount)
                {
                    promoDiscount = Math.Min(promotion.DiscountValue, pricing.ServicePrice);
                }

                pricing = (
                    ServicePrice: pricing.ServicePrice,
                    ConvenienceFee: pricing.ConvenienceFee,
                    DiscountAmount: pricing.DiscountAmount + promoDiscount,
                    TotalPrice: Math.Max(0, pricing.TotalPrice - promoDiscount)
                );

                promoCode.UsedCount++;
                _context.PromotionCodes.Update(promoCode);
                await _context.SaveChangesAsync();
            }
        }

        var address = (await _addressRepository.GetAsync(a => a.UserId == customer.Id && a.Street == model.AddressLine)).FirstOrDefault();
        if (address is null)
        {
            address = new Shatbly.Models.Address
            {
                UserId = customer.Id,
                City = "Cairo",
                District = model.AddressLabel ?? "Home",
                Street = model.AddressLine ?? "Default Street",
                IsDefault = true
            };
            await _addressRepository.CreateAsync(address);
            await _addressRepository.CommitAsync();
        }

        var parentBooking = new Shatbly.Models.Booking
        {
            ClientId = customer.Id,
            WorkerId = workerProfile.Id,
            AddressId = address.Id,
            ScheduledAt = resolvedScheduledAt!.Value,
            DurationHours = 1,
            TotalPrice = pricing.TotalPrice,
            DiscountAmt = pricing.DiscountAmount,
            Status = Shatbly.Models.BookingStatus.Pending,
            PromoCodeId = promoCode?.Id,
            CreatedAt = DateTime.UtcNow
        };
        await _bookingRepository.CreateAsync(parentBooking);
        await _bookingRepository.CommitAsync();

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
            AddressLabel = model.AddressLabel ?? "Home",
            AddressLine = model.AddressLine ?? "",
            Notes = model.Notes,
            BookingId = parentBooking.Id,
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

        // :      
        await _orderRepository.CreateAsync(order);
        await _orderRepository.CommitAsync();

        return new BookingCreateResult
        {
            Succeeded = true,
            BookingId = order.Id,
            SuccessMessage = _localizer["BookingCreatedSuccess"].Value
        };
    }
    public async Task<BookingDetailsViewModel?> GetDetailsAsync(int id)
    {
        var booking = (await _orderRepository.GetAsync(o => o.Id == id, new System.Linq.Expressions.Expression<System.Func<Order, object>>[] { o => o.Booking })).FirstOrDefault();
        if (booking is null)
        {
            return null;
        }

        await PopulateNavigationAsync(booking);

        var reviewList = await _reviewRepository.GetAsync(r => r.BookingId == id);
        var hasReview = reviewList != null && System.Linq.Enumerable.Any(reviewList);

        return new BookingDetailsViewModel
        {
            Booking = booking,
            RefundPreview = CalculateRefund(booking),
            CanCancel = CanManageBooking(booking),
            CanReschedule = CanManageBooking(booking),
            HasReview = hasReview,
            Review = reviewList?.FirstOrDefault()
        };
    }

    public async Task<BookingActionResult> RescheduleAsync(int id, string scheduledAt)
    {
        var booking = (await _orderRepository.GetAsync(o => o.Id == id, new System.Linq.Expressions.Expression<System.Func<Order, object>>[] { o => o.Booking })).FirstOrDefault();
        if (booking is null)
        {
            return new BookingActionResult { NotFound = true };
        }

        if (!DateTime.TryParse(scheduledAt, out var parsedSlot) || booking.WorkerId is null)
        {
            return FailAction(id, _localizer["ChooseValidSlot"].Value);
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
            return FailAction(id, _localizer["SlotNoLongerAvailable"].Value);
        }

        booking.ScheduledAt = validSlot;
        booking.Status = OrderStatuses.Rescheduled;
        booking.WorkerResponseDeadlineUtc = DateTime.UtcNow.AddMinutes(WorkerResponseWindowMinutes);

        if (booking.Booking is not null)
        {
            booking.Booking.ScheduledAt = validSlot;
            booking.Booking.Status = Shatbly.Models.BookingStatus.Pending;
        }

         _orderRepository.Update(booking);
        await _orderRepository.CommitAsync();
        //await _userManager.UpdateAsync(booking);


        return new BookingActionResult
        {
            Succeeded = true,
            BookingId = id,
            Message = _localizer["BookingRescheduled"].Value
        };
    }

    public async Task<BookingActionResult> CancelAsync(int id, string? cancellationReason)
    {
        var booking = (await _orderRepository.GetAsync(o => o.Id == id, new System.Linq.Expressions.Expression<System.Func<Order, object>>[] { o => o.Booking })).FirstOrDefault();
        if (booking is null)
        {
            return new BookingActionResult { NotFound = true };
        }

        booking.Status = OrderStatuses.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancellationReason = string.IsNullOrWhiteSpace(cancellationReason)
            ? _localizer["CancelledByCustomer"].Value
            : cancellationReason.Trim();
        booking.RefundAmount = CalculateRefund(booking);

        if (booking.Booking is not null)
        {
            booking.Booking.Status = Shatbly.Models.BookingStatus.Cancelled;
        }

        _orderRepository.Update(booking);
        await _orderRepository.CommitAsync();
        //await _userManager.UpdateAsync(booking);


        return new BookingActionResult
        {
            Succeeded = true,
            BookingId = id,
            Message = string.Format(_localizer["BookingCancelled"].Value, booking.RefundAmount.ToString("0.00"))
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
        var nameParts = model.CustomerName?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var firstName = nameParts.Length > 0 ? nameParts[0] : "Customer";
        var lastName = nameParts.Length > 1 ? string.Join(' ', nameParts.Skip(1)) : "User";
        var phoneVal = model.CustomerPhone ?? "";

        if (customer is null)
        {
            customer = new User
            {
                UserName = model.CustomerEmail,
                Name = model.CustomerName ?? "",
                Email = model.CustomerEmail,
                PhoneNumber = phoneVal,
                FName = firstName,
                LName = lastName,
                Phone = phoneVal
             };

            var createResult = await _userManager.CreateAsync(customer);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException(string.Format(_localizer["CustomerCreationFailed"].Value, errors));
            }

            await _userManager.AddToRoleAsync(customer, SD.ROLE_CUSTOMER);
            return customer;
        }

        customer.Name = model.CustomerName ?? "";
        customer.PhoneNumber = phoneVal;
        customer.FName = firstName;
        customer.LName = lastName;
        customer.Phone = phoneVal;
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

        var workerProfile = (await _workerProfileRepository.GetAsync(
            wp => wp.UserId == workerId,
            includes: new System.Linq.Expressions.Expression<System.Func<WorkerProfile, object>>[] { wp => wp.Availabilities }
        )).FirstOrDefault();

        if (workerProfile == null)
        {
            return result;
        }

        var availabilities = workerProfile.Availabilities ?? new List<Avalability>();

        for (var dayOffset = 0; dayOffset < 10; dayOffset++)
        {
            var date = now.Date.AddDays(dayOffset);
            var slots = new List<string>();

            var dayOfWeekModel = (Shatbly.Models.DayOfWeek)date.DayOfWeek;
            var dailyAvailabilities = availabilities.Where(a => a.DayOfWeek == dayOfWeekModel).ToList();

            foreach (var avail in dailyAvailabilities)
            {
                var currentSlotStart = avail.StartTime;
                while (currentSlotStart + TimeSpan.FromHours(1) <= avail.EndTime)
                {
                    var slot = date.Add(currentSlotStart);

                    if (slot <= now.Add(SlotLeadTime))
                    {
                        currentSlotStart = currentSlotStart.Add(TimeSpan.FromHours(1));
                        continue;
                    }

                    if (workerBookings.Any(existing => existing == slot))
                    {
                        currentSlotStart = currentSlotStart.Add(TimeSpan.FromHours(1));
                        continue;
                    }

                    slots.Add(slot.ToString("HH:mm"));
                    currentSlotStart = currentSlotStart.Add(TimeSpan.FromHours(1));
                }
            }

            if (slots.Count > 0)
            {
                result[date.ToString("yyyy-MM-dd")] = slots.OrderBy(s => s).ToList();
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
    public async Task<bool> MarkAsPaidAsync(int bookingId)
    { 
    var booking = await _orderRepository.GetOneAsync(o => o.Id == bookingId, new System.Linq.Expressions.Expression<System.Func<Order, object>>[] { o => o.Booking });
        if (booking != null)
        { 
        booking.PaymentStatus = PaymentStatuses.Paid;
            booking.Status = OrderStatuses.Confirmed;
            if (booking.Booking != null)
            {
                booking.Booking.Status = Shatbly.Models.BookingStatus.Confirmed;
            }
             _orderRepository.Update(booking);
            await _orderRepository.CommitAsync();
            return true;
        }
        return false;
    }
    public async Task<bool> AddReviewAsync(Review review)
    { 
    var booking = await _orderRepository.GetOneAsync(o => o.Id == review.BookingId);
        if (booking != null && booking.Status == OrderStatuses.Completed)
        {
            await _reviewRepository.CreateAsync(review);
            await _reviewRepository.CommitAsync();
            return true;
        }
        return false;
    }
    public async Task<bool> RaiseDisputeAsync(int bookingId , string reason, string raisedById)
    {
        var booking = await _orderRepository.GetOneAsync(o => o.Id == bookingId, new System.Linq.Expressions.Expression<System.Func<Order, object>>[] { o => o.Booking });
        if (booking == null)
        {
            return false;
        }

        if (booking.Booking != null)
        {
            booking.Booking.Status = BookingStatus.Disputed;
        }
        _orderRepository.Update(booking);
        await _orderRepository.CommitAsync();

        if (booking.BookingId > 0)
        {
            string againstId = (raisedById == booking.UserId) ? booking.WorkerId : booking.UserId;

            var dispute = new Dipuste
            {
                BookingId = booking.BookingId,
                Reason = reason.Length > 255 ? reason.Substring(0, 255) : reason,
                Description = reason,
                RaisedById = raisedById,
                AgainstId = againstId ?? string.Empty,
                Status = DisputeStatus.Open,
                Resolution = string.Empty,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Disputes.AddAsync(dispute);
            await _context.SaveChangesAsync();
        }

        return true;
    }
    public async Task<List<Order>> GetCustomerOrdersAsync(string userId)
    {
        var orders = await _orderRepository.GetAsync(
            o => o.UserId == userId,
            includes: new System.Linq.Expressions.Expression<System.Func<Order, object>>[] 
            { 
                o => o.Service!, 
                o => o.Worker!,
                o => o.Booking
            },
            tracking: false
        );
        return orders.OrderByDescending(o => o.CreatedAt).ToList();
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
