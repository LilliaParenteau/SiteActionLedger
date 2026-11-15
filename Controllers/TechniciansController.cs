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
            if (ModelState.IsValid)
            {
                _context.Technicians.Add(technician);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(technician);
        }
    }
}
