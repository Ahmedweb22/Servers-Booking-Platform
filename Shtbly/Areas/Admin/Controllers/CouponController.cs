using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Shtbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN} , {SD.ROLE_SUPER_ADMIN}")]
    public class CouponController : Controller
    {
        private readonly IRepository<Coupon> _couponRepo;
        private readonly IRepository<ServiceCategory> _serviceCategoryRepo;
        private readonly IStringLocalizer<CouponController> _localizer;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        public CouponController(IRepository<Coupon> couponRepo,
            IStringLocalizer<CouponController> localizer, IStringLocalizer<SharedResource> sharedLocalizer, IRepository<ServiceCategory> serviceCategoryRepo)
        {
            _couponRepo = couponRepo;
            _localizer = localizer;
            _sharedLocalizer = sharedLocalizer;
            _serviceCategoryRepo = serviceCategoryRepo;
        }

        public async Task<IActionResult> Index(string? coupon, int Page = 1)
        {
            var coupons = await _couponRepo.GetAsync(tracking: false);

            if (Page < 1)
                Page = 1;

            int currentPage = Page;
            double TotalPages = Math.Ceiling(coupons.Count() / 5.0);//3

            coupons = coupons.Skip((currentPage - 1) * 5).Take(5).ToList();

            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = TotalPages;

            if (coupon is not null)
                coupons = coupons.Where(c => c.Code.Contains(coupon)).ToList();

            return View(new CouponsVM
            {
                Coupons = coupons,
                CurrentPage = currentPage,
                TotalPages = TotalPages
            });
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (coupon.DiscountType == DiscountType.Percentage && coupon.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Percentage discount cannot exceed 100%.");
            }
            if (coupon.MaxUses < 1)
            {
                ModelState.AddModelError("MaxUses", "Max Uses must be at least 1.");
            }
            if (coupon.ValidFrom > coupon.ValidUntil)
            {
                ModelState.AddModelError("ValidUntil", "Valid Until date must be after Valid From date.");
            }

            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = _sharedLocalizer["PleaseCorrectErrors"].Value;
                return View(coupon);
            }

            //if (coupon.Category.IsActive)
            //{
            //    TempData["error-notification"] = "this Category is not found" ;
            //    return RedirectToAction("Create");
            //}

            if (coupon.CategoryId.HasValue && coupon.CategoryId.Value != 0)
            {
                var category = await _serviceCategoryRepo.GetOneAsync(c=>c.Id == coupon.CategoryId.Value);
                if (category == null)
                {
                    TempData["worning-notification"] = "this Category is not found";
                    return RedirectToAction("Create");
                }
            }
            else
            {
                coupon.CategoryId = null; 
            }

            await _couponRepo.CreateAsync(coupon);
            await _couponRepo.CommitAsync();
            TempData["success-notification"] = _localizer["CouponCreatedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var coupon = await _couponRepo.GetOneAsync(c => c.Id == id, tracking: false);
            if (coupon is null)
            {
                TempData["error-notification"] = _localizer["CouponNotFound"].Value;
                return NotFound();
            }
            return View(coupon);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Coupon coupon)
        {
            if (coupon.DiscountType == DiscountType.Percentage && coupon.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Percentage discount cannot exceed 100%.");
            }
            if (coupon.MaxUses < 1)
            {
                ModelState.AddModelError("MaxUses", "Max Uses must be at least 1.");
            }
            if (coupon.ValidFrom > coupon.ValidUntil)
            {
                ModelState.AddModelError("ValidUntil", "Valid Until date must be after Valid From date.");
            }

            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = _sharedLocalizer["PleaseCorrectErrors"].Value;
                return View(coupon);
            }
            _couponRepo.Update(coupon);
            await _couponRepo.CommitAsync();
            TempData["success-notification"] = _localizer["CouponUpdatedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _couponRepo.GetOneAsync(c => c.Id == id);
            if (coupon is null)
            {
                TempData["error-notification"] = _localizer["CouponNotFound"].Value;
                return NotFound();
            }
            _couponRepo.Delete(coupon);
            await _couponRepo.CommitAsync();
            TempData["success-notification"] = _localizer["CouponDeletedSuccess"].Value;
            return Ok();
        }
    }
}
