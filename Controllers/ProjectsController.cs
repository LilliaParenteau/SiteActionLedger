using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SiteActionLedger.Data;
using SiteActionLedger.Models;

namespace SiteActionLedger.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly SiteActionLedgerContext _context;

        public ProjectsController(SiteActionLedgerContext context)
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

        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            ViewBag.SiteIssues = await _context.ServiceRequests
                .Where(r => r.ProjectId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(project);
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

            if (!ValidStatus(project.Status))
            {
                ModelState.AddModelError(
                    "Status", "Select a valid project status.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCustomers(project.CustomerId);
                return View(project);
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null)
            {
                return NotFound();
            }

            await LoadCustomers(project.CustomerId);
            return View(project);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(
            int id,
            [Bind("Id,Name,SiteAddress,Description,Status,CustomerId")] Project project)
        {
            if (id != project.Id)
            {
                return BadRequest();
            }

            var existingProject = await _context.Projects.FindAsync(id);

            if (existingProject == null)
            {
                return NotFound();
            }

            bool customerExists = await _context.Customers
                .AnyAsync(c => c.Id == project.CustomerId);

            if (!customerExists)
            {
                ModelState.AddModelError(
                    "CustomerId", "Select a valid client.");
            }

            if (!ValidStatus(project.Status))
            {
                ModelState.AddModelError(
                    "Status", "Select a valid project status.");
            }

            if (existingProject.CustomerId != project.CustomerId)
            {
                bool hasSiteIssues = await _context.ServiceRequests
                    .AnyAsync(r => r.ProjectId == id);

                if (hasSiteIssues)
                {
                    ModelState.AddModelError(
                        "CustomerId",
                        "This project has existing site issues. " +
                        "Reassign those issues before changing the client.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadCustomers(project.CustomerId);
                return View("Edit", project);
            }

            existingProject.Name = project.Name;
            existingProject.SiteAddress = project.SiteAddress;
            existingProject.Description = project.Description;
            existingProject.Status = project.Status;
            existingProject.CustomerId = project.CustomerId;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool ValidStatus(string status)
        {
            return status == "Active" ||
                   status == "On Hold" ||
                   status == "Completed";
        }

        private async Task LoadCustomers(int? selectedId = null)
        {
            var customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Customers = new SelectList(
                customers, "Id", "Name", selectedId);
        }
    }
}
