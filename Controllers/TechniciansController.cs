using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiteActionLedger.Data;
using SiteActionLedger.Models;

namespace SiteActionLedger.Controllers
{
    public class TechniciansController : Controller
    {
        private readonly SiteActionLedgerContext _context;

        public TechniciansController(SiteActionLedgerContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var technicians = _context.Technicians.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                technicians = technicians.Where(t =>
                    t.Name.Contains(search) ||
                    t.Specialty.Contains(search) ||
                    t.Phone.Contains(search));
            }

            ViewBag.Search = search;

            return View(await technicians
                .OrderBy(t => t.Name)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var technician = await _context.Technicians.FindAsync(id);

            if (technician == null)
            {
                return NotFound();
            }

            ViewBag.WorkOrders = await _context.WorkOrders
                .Include(w => w.SiteIssue)
                .ThenInclude(i => i!.Customer)
                .Where(w => w.TechnicianId == id)
                .OrderByDescending(w => w.Id)
                .ToListAsync();

            return View(technician);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Specialty,Phone")] Technician technician)
        {
            if (string.IsNullOrWhiteSpace(technician.Phone))
            {
                technician.Phone = "";
                ModelState.Remove(nameof(Technician.Phone));
            }

            if (!ModelState.IsValid)
            {
                return View(technician);
            }

            _context.Technicians.Add(technician);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var technician = await _context.Technicians.FindAsync(id);

            if (technician == null)
            {
                return NotFound();
            }

            return View(technician);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Specialty,Phone")] Technician technician)
        {
            if (id != technician.Id)
            {
                return BadRequest();
            }

            var existingTechnician =
                await _context.Technicians.FindAsync(id);

            if (existingTechnician == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(technician.Phone))
            {
                technician.Phone = "";
                ModelState.Remove(nameof(Technician.Phone));
            }

            if (!ModelState.IsValid)
            {
                return View(technician);
            }

            existingTechnician.Name = technician.Name;
            existingTechnician.Specialty = technician.Specialty;
            existingTechnician.Phone = technician.Phone;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var technician = await _context.Technicians.FindAsync(id);

            if (technician == null)
            {
                return NotFound();
            }

            return View(technician);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var technician = await _context.Technicians.FindAsync(id);

            if (technician == null)
            {
                return NotFound();
            }

            bool hasWorkOrders = await _context.WorkOrders
                .AnyAsync(w => w.TechnicianId == id);

            if (hasWorkOrders)
            {
                ModelState.AddModelError("",
                    "This technician is assigned to a work order and cannot be deleted.");

                return View("Delete", technician);
            }

            _context.Technicians.Remove(technician);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}