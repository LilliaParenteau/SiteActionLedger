using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpsPilot.Data;
using OpsPilot.Models;

namespace OpsPilot.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly OpsPilotContext _context;

        public ProjectsController(OpsPilotContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects
                .Include(p => p.Customer)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(projects);
        }

        public async Task<IActionResult> Create()
        {
            await LoadCustomers();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,SiteAddress,Description,Status,CustomerId")] Project project)
        {
            bool customerExists = await _context.Customers
                .AnyAsync(c => c.Id == project.CustomerId);

            if (!customerExists)
            {
                ModelState.AddModelError(
                    "CustomerId", "Select a valid client.");
            }

            if (project.Status != "Active" &&
                project.Status != "On Hold" &&
                project.Status != "Completed")
            {
                ModelState.AddModelError(
                    "Status", "Select a valid project status.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCustomers();
                return View(project);
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCustomers()
        {
            var customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Customers = new SelectList(
                customers, "Id", "Name");
        }
    }
}
