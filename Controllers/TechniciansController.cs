using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsPilot.Data;
using OpsPilot.Models;

namespace OpsPilot.Controllers
{
    public class TechniciansController : Controller
    {
        private readonly OpsPilotContext _context;

        public TechniciansController(OpsPilotContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var technicians = await _context.Technicians
                .OrderBy(t => t.Name)
                .ToListAsync();

            return View(technicians);
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
