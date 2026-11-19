using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SiteActionLedger.Data;
using SiteActionLedger.Models;

namespace SiteActionLedger.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly SiteActionLedgerContext _context;

        public ServiceRequestsController(SiteActionLedgerContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? search, string? status, string? priority)
        {
            var requests = _context.ServiceRequests
                .Include(r => r.Customer)
                .Include(r => r.Project)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                requests = requests.Where(r =>
                    r.Title.Contains(search) ||
                    r.Description.Contains(search) ||
                    r.Customer!.Name.Contains(search) ||
                    (r.Project != null &&
                     r.Project.Name.Contains(search)));
            }

            if (status == "Open" ||
                status == "In Progress" ||
                status == "Completed")
            {
                requests = requests.Where(r => r.Status == status);
            }

            if (priority == "Low" ||
                priority == "Normal" ||
                priority == "High")
            {
                requests = requests.Where(r => r.Priority == priority);
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Priority = priority;

            return View(await requests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.ServiceRequests
                .Include(r => r.Customer)
                .Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            ViewBag.WorkOrderId = await _context.WorkOrders
                .Where(w => w.ServiceRequestId == id)
                .Select(w => (int?)w.Id)
                .FirstOrDefaultAsync();

            return View(request);
        }

        public async Task<IActionResult> Create()
        {
            await LoadProjects();
            return View(new ServiceRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title,Description,Priority,ProjectId")] ServiceRequest request)
        {
            if (!ValidPriority(request.Priority))
            {
                ModelState.AddModelError(
                    "Priority", "Select a valid priority.");
            }

            Project? project = null;

            if (request.ProjectId.HasValue)
            {
                project = await _context.Projects
                    .FindAsync(request.ProjectId.Value);
            }

            if (project == null)
            {
                ModelState.AddModelError(
                    "ProjectId", "Select a valid construction project.");
            }
            else
            {
                request.CustomerId = project.CustomerId;
                ModelState.Remove(nameof(ServiceRequest.CustomerId));
            }

            if (!ModelState.IsValid)
            {
                await LoadProjects(request.ProjectId);
                return View(request);
            }

            request.Status = "Open";
            request.CreatedAt = DateTime.UtcNow;

            _context.ServiceRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var request = await _context.ServiceRequests
                .Include(r => r.Customer)
                .Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            await LoadProjects(request.ProjectId);

            return View(request);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id)
        {
            var request = await _context.ServiceRequests
                .Include(r => r.Customer)
                .Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            int? originalProjectId = request.ProjectId;

            bool updated = await TryUpdateModelAsync(
                request,
                "",
                r => r.Title,
                r => r.Description,
                r => r.Priority,
                r => r.Status,
                r => r.ProjectId
            );

            if (!ValidPriority(request.Priority))
            {
                ModelState.AddModelError(
                    "Priority", "Select a valid priority.");
            }

            if (!ValidStatus(request.Status))
            {
                ModelState.AddModelError(
                    "Status", "Select a valid status.");
            }

            if (request.ProjectId.HasValue)
            {
                var project = await _context.Projects
                    .FindAsync(request.ProjectId.Value);

                if (project == null)
                {
                    ModelState.AddModelError(
                        "ProjectId", "Select a valid construction project.");
                }
                else
                {
                    request.CustomerId = project.CustomerId;
                }
            }
            else if (originalProjectId.HasValue)
            {
                ModelState.AddModelError(
                    "ProjectId", "Select a construction project.");
            }

            if (!updated || !ModelState.IsValid)
            {
                await LoadProjects(request.ProjectId);
                return View("Edit", request);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool ValidPriority(string priority)
        {
            return priority == "Low" ||
                   priority == "Normal" ||
                   priority == "High";
        }

        private bool ValidStatus(string status)
        {
            return status == "Open" ||
                   status == "In Progress" ||
                   status == "Completed";
        }

        private async Task LoadProjects(int? selectedId = null)
        {
            var projects = await _context.Projects
                .Include(p => p.Customer)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var options = projects.Select(p => new
            {
                p.Id,
                DisplayName = $"{p.Name} - {p.Customer?.Name}"
            });

            ViewBag.Projects = new SelectList(
                options, "Id", "DisplayName", selectedId);
        }
    }
}
