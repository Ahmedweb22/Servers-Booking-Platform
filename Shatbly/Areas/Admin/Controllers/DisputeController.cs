using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shatbly.Models;
using Shatbly.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Shatbly.DataAccess;

namespace Shatbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN}")]
    public class DisputeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DisputeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            var disputes = await _context.Disputes
                .Include(d => d.Booking)
                .Include(d => d.RaisedBy)
                .Include(d => d.Against)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

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
            var dispute = await _context.Disputes
                .Include(d => d.Booking)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dispute == null)
            {
                return NotFound();
            }

            dispute.Status = DisputeStatus.Resolved;
            dispute.Resolution = string.IsNullOrWhiteSpace(resolution) ? "Resolved by support." : resolution;

            _context.Disputes.Update(dispute);
            await _context.SaveChangesAsync();

            TempData["success-notification"] = "Dispute resolved successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
