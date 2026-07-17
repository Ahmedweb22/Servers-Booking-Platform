using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shtbly.Models;
using Shtbly.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Shtbly.DataAccess;

namespace Shtbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN}")]
    public class DisputeController : Controller
    {
        private readonly Shtbly.UnitOfWork.IUnitOfWork _unitOfWork;

        public DisputeController(Shtbly.UnitOfWork.IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            var allDisputes = await _unitOfWork.Disputes.GetAsync(
                includes: new System.Linq.Expressions.Expression<System.Func<Dipuste, object>>[] 
                { 
                    d => d.Booking!, 
                    d => d.RaisedBy!, 
                    d => d.Against! 
                }, 
                tracking: false
            );
            var disputes = allDisputes.OrderByDescending(d => d.CreatedAt).ToList();

            if (page < 1) page = 1;
            int pageSize = 10;
            double totalPages = Math.Ceiling(disputes.Count / (double)pageSize);
            var pagedDisputes = disputes.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedDisputes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id, string resolution)
        {
            var dispute = await _unitOfWork.Disputes.GetOneAsync(
                expression: d => d.Id == id,
                includes: new System.Linq.Expressions.Expression<System.Func<Dipuste, object>>[] { d => d.Booking! }
            );

            if (dispute == null)
            {
                return NotFound();
            }

            dispute.Status = DisputeStatus.Resolved;
            dispute.Resolution = string.IsNullOrWhiteSpace(resolution) ? "Resolved by support." : resolution;

            _unitOfWork.Disputes.Update(dispute);
            await _unitOfWork.CommitAsync();

            TempData["success-notification"] = "Dispute resolved successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
