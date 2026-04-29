using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shatbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    //[Authorize(Roles = $"{SD.ROLE_ADMIN} , {SD.ROLE_SUPER_ADMIN}")]
    public class PromotionCodeController : Controller
    {
        private readonly IRepository<PromotionCode> _promotionCodeRepository;
        private readonly IRepository<Promotion> _promotionRepository;

        public PromotionCodeController(IRepository<PromotionCode> promotionCodeRepository, IRepository<Promotion> promotionRepository)
        {
            _promotionCodeRepository = promotionCodeRepository;
            _promotionRepository = promotionRepository;
        }
        public async Task<IActionResult> Index(string? code, int page = 1)
        {
            var promotionCodes = await _promotionCodeRepository.GetAsync(includes: [p => p.Promotion], tracking: false);
            if (code is not null)
            {
                promotionCodes = promotionCodes.Where(p => p.Code.Contains(code)).ToList();
            }
            if (page < 1) page = 1;

            int currentPage = page;
            double totalPages = Math.Ceiling(promotionCodes.Count() / 5.0);
            promotionCodes = promotionCodes.Skip((page - 1) * 5).Take(5).ToList();

            return View(new PromotionCodesVM
            {
                PromotionCodes = promotionCodes.AsEnumerable(),
                CurrentPage = currentPage,
                TotalPages = totalPages
            });
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var promotions = await _promotionRepository.GetAsync(tracking: false);

            return View(new PromotionCodeCreateVM
            {
                PromotionCode = new PromotionCode(),
                Promotions = promotions.AsEnumerable(),
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(PromotionCodeCreateVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Promotions = await _promotionRepository.GetAsync(tracking: false);
                return View(vm);
            }
            await _promotionCodeRepository.CreateAsync(vm.PromotionCode);
            await _promotionCodeRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] int id)
        {
            var promotionCode = await _promotionCodeRepository.GetOneAsync(p => p.Id == id);

            if (promotionCode is null) return NotFound();

            var promotions = await _promotionRepository.GetAsync(tracking: false);

            return View(new PromotionCodeUpdateResponseVM
            {
                PromotionCode = promotionCode,
                Promotions = promotions.AsEnumerable(),
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PromotionCodeUpdateResponseVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Promotions = await _promotionRepository.GetAsync(tracking: false);
                return View(vm);
            }

            _promotionCodeRepository.Update(vm.PromotionCode);
            await _promotionCodeRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var promotionCode = await _promotionCodeRepository.GetOneAsync(p => p.Id == id);
            if (promotionCode is null) return NotFound();

            _promotionCodeRepository.Delete(promotionCode);
            await _promotionCodeRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
