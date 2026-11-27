using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SiteActionTrace.Data;
using SiteActionTrace.Models;

namespace SiteActionTrace.Controllers
{
    public class SiteIssuesController : Controller
    {
        private readonly SiteActionTraceContext _context;

        public SiteIssuesController(SiteActionTraceContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? search,
            string? status,
            string? priority,
            string? category)
        {
            var issues = _context.SiteIssues
                .Include(i => i.Customer)
                .Include(i => i.Project)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                issues = issues.Where(i =>
                    i.Title.Contains(search) ||
                    i.Description.Contains(search) ||
                    i.Customer!.Name.Contains(search) ||
                    (i.Project != null &&
                     i.Project.Name.Contains(search)) ||
                    (i.SiteArea != null &&
                     i.SiteArea.Contains(search)));
            }

            if (ValidStatus(status))
            {
                issues = issues.Where(i => i.Status == status);
            }

            if (ValidPriority(priority))
            {
                issues = issues.Where(i => i.Priority == priority);
            }

            if (!string.IsNullOrWhiteSpace(category) &&
                ValidCategory(category))
            {
                issues = issues.Where(i => i.Category == category);
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.Category = category;

            return View(await issues
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var issue = await _context.SiteIssues
                .Include(i => i.Customer)
                .Include(i => i.Project)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
            {
                return NotFound();
            }

            ViewBag.WorkOrderId = await _context.WorkOrders
                .Where(w => w.SiteIssueId == id)
                .Select(w => (int?)w.Id)
                .FirstOrDefaultAsync();

            return View(issue);
        }

        public async Task<IActionResult> Create(int? projectId)
        {
            await LoadProjects(projectId);

            return View(new SiteIssue
            {
                ProjectId = projectId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title,Description,Category,SiteArea,Priority,ProjectId")]
            SiteIssue issue)
        {
            if (!ValidPriority(issue.Priority))
            {
                ModelState.AddModelError(
                    nameof(SiteIssue.Priority),
                    "Select a valid priority.");
            }

            if (!ValidCategory(issue.Category))
            {
                ModelState.AddModelError(
                    nameof(SiteIssue.Category),
                    "Select a valid issue category.");
            }

            Project? project = null;

            if (issue.ProjectId.HasValue)
            {
                project = await _context.Projects
                    .FindAsync(issue.ProjectId.Value);
            }

            if (project == null)
            {
                ModelState.AddModelError(
                    nameof(SiteIssue.ProjectId),
                    "Select a valid construction project.");
            }
            else
            {
                issue.CustomerId = project.CustomerId;
                ModelState.Remove(nameof(SiteIssue.CustomerId));
            }

            if (!ModelState.IsValid)
            {
                await LoadProjects(issue.ProjectId);
                return View(issue);
            }

            issue.Status = SiteIssueStatuses.Open;
            issue.CreatedAt = DateTime.UtcNow;

            _context.SiteIssues.Add(issue);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var issue = await _context.SiteIssues
                .Include(i => i.Customer)
                .Include(i => i.Project)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
            {
                return NotFound();
            }

            var workOrder = await _context.WorkOrders
                .Where(w => w.SiteIssueId == id)
                .Select(w => new { w.Id, w.Status })
                .FirstOrDefaultAsync();

            ViewBag.LinkedWorkOrderId = workOrder?.Id;

            if (workOrder != null)
            {
                ViewBag.LinkedWorkOrderStatus =
                    workOrder.Status == WorkOrderStatuses.Pending
                        ? SiteIssueStatuses.Open
                        : workOrder.Status;
            }

            await LoadProjects(issue.ProjectId);

            return View(issue);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id, string? status)
        {
            var issue = await _context.SiteIssues
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
            {
                return NotFound();
            }

            int? originalProjectId = issue.ProjectId;

            var workOrder = await _context.WorkOrders
                .Where(w => w.SiteIssueId == id)
                .Select(w => new { w.Id, w.Status })
                .FirstOrDefaultAsync();

            bool updated = await TryUpdateModelAsync(
                issue,
                "",
                i => i.Title,
                i => i.Description,
                i => i.Category,
                i => i.SiteArea,
                i => i.Priority,
                i => i.ProjectId
            );

            if (!ValidPriority(issue.Priority))
            {
                ModelState.AddModelError(
                    nameof(SiteIssue.Priority),
                    "Select a valid priority.");
            }

            if (!ValidCategory(issue.Category))
            {
                ModelState.AddModelError(
                    nameof(SiteIssue.Category),
                    "Select a valid issue category.");
            }

            if (workOrder == null)
            {
                if (!ValidStatus(status))
                {
                    ModelState.AddModelError(
                        nameof(SiteIssue.Status),
                        "Select a valid status.");
                }
                else
                {
                    issue.Status = status!;
                }
            }

            if (issue.ProjectId.HasValue)
            {
                var project = await _context.Projects
                    .FindAsync(issue.ProjectId.Value);

                if (project == null)
                {
                    ModelState.AddModelError(
                        nameof(SiteIssue.ProjectId),
                        "Select a valid construction project.");
                }
                else
                {
                    issue.CustomerId = project.CustomerId;
                }
            }
            else if (originalProjectId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(SiteIssue.ProjectId),
                    "Select a construction project.");
            }

            if (!updated || !ModelState.IsValid)
            {
                ViewBag.LinkedWorkOrderId = workOrder?.Id;

                if (workOrder != null)
                {
                    ViewBag.LinkedWorkOrderStatus =
                        workOrder.Status == WorkOrderStatuses.Pending
                            ? SiteIssueStatuses.Open
                            : workOrder.Status;
                }

                await LoadProjects(issue.ProjectId);

                return View("Edit", issue);
            }

            if (workOrder != null)
            {
                issue.Status = workOrder.Status == WorkOrderStatuses.Pending
                    ? SiteIssueStatuses.Open
                    : workOrder.Status;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private static bool ValidPriority(string? priority)
        {
            return priority == "Low" ||
                   priority == "Normal" ||
                   priority == "High";
        }

        private static bool ValidStatus(string? status)
        {
            return status == SiteIssueStatuses.Open ||
                   status == SiteIssueStatuses.InProgress ||
                   status == SiteIssueStatuses.Completed;
        }

        private static bool ValidCategory(string? category)
        {
            return string.IsNullOrWhiteSpace(category) ||
                   category == "Electrical" ||
                   category == "Plumbing" ||
                   category == "HVAC" ||
                   category == "Structural" ||
                   category == "Interior Finishes" ||
                   category == "Safety" ||
                   category == "Other";
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