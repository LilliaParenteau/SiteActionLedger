using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SiteActionTrace.Data;
using SiteActionTrace.Models;

namespace SiteActionTrace.Controllers
{
    public class WorkOrdersController : Controller
    {
        private readonly SiteActionTraceContext _context;

        public WorkOrdersController(SiteActionTraceContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            var workOrders = _context.WorkOrders
                .Include(w => w.SiteIssue)
                    .ThenInclude(i => i!.Customer)
                .Include(w => w.Technician)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                workOrders = workOrders.Where(w =>
                    w.SiteIssue!.Title.Contains(search) ||
                    w.SiteIssue.Customer!.Name.Contains(search) ||
                    (w.Technician != null &&
                     w.Technician.Name.Contains(search)));
            }

            if (status == WorkOrderStatuses.Pending ||
                status == WorkOrderStatuses.InProgress ||
                status == WorkOrderStatuses.Completed)
            {
                workOrders = workOrders.Where(w => w.Status == status);
            }

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(await workOrders
                .OrderByDescending(w => w.Id)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var workOrder = await _context.WorkOrders
                .Include(w => w.SiteIssue)
                    .ThenInclude(i => i!.Customer)
                .Include(w => w.Technician)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workOrder == null)
            {
                return NotFound();
            }

            return View(workOrder);
        }

        public async Task<IActionResult> Create(int? siteIssueId)
        {
            await LoadSiteIssues(siteIssueId);
            await LoadTechnicians();

            return View(new WorkOrder
            {
                SiteIssueId = siteIssueId ?? 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("SiteIssueId,ScheduledDate,Notes,TechnicianId")]
            WorkOrder workOrder)
        {
            var issue = await _context.SiteIssues
                .FindAsync(workOrder.SiteIssueId);

            if (issue == null)
            {
                ModelState.AddModelError(
                    nameof(WorkOrder.SiteIssueId),
                    "Select a valid site issue.");
            }

            bool alreadyAssigned = await _context.WorkOrders
                .AnyAsync(w => w.SiteIssueId == workOrder.SiteIssueId);

            if (alreadyAssigned)
            {
                ModelState.AddModelError(
                    nameof(WorkOrder.SiteIssueId),
                    "This site issue already has a work order.");
            }

            if (workOrder.TechnicianId.HasValue)
            {
                bool technicianExists = await _context.Technicians
                    .AnyAsync(t => t.Id == workOrder.TechnicianId.Value);

                if (!technicianExists)
                {
                    ModelState.AddModelError(
                        nameof(WorkOrder.TechnicianId),
                        "Select a valid technician.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadSiteIssues(workOrder.SiteIssueId);
                await LoadTechnicians();

                return View(workOrder);
            }

            workOrder.Status = WorkOrderStatuses.Pending;
            issue!.Status = SiteIssueStatuses.Open;

            _context.WorkOrders.Add(workOrder);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var workOrder = await _context.WorkOrders
                .Include(w => w.SiteIssue)
                    .ThenInclude(i => i!.Customer)
                .Include(w => w.Technician)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workOrder == null)
            {
                return NotFound();
            }

            await LoadTechnicians();

            return View(workOrder);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id)
        {
            var workOrder = await _context.WorkOrders
                .Include(w => w.SiteIssue)
                    .ThenInclude(i => i!.Customer)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workOrder == null)
            {
                return NotFound();
            }

            bool updated = await TryUpdateModelAsync(
                workOrder,
                "",
                w => w.ScheduledDate,
                w => w.Status,
                w => w.Notes,
                w => w.TechnicianId,
                w => w.ResolutionNotes
            );

            if (workOrder.Status != WorkOrderStatuses.Pending &&
                workOrder.Status != WorkOrderStatuses.InProgress &&
                workOrder.Status != WorkOrderStatuses.Completed)
            {
                ModelState.AddModelError(
                    nameof(WorkOrder.Status),
                    "Select a valid status.");
            }

            if (workOrder.TechnicianId.HasValue)
            {
                bool technicianExists = await _context.Technicians
                    .AnyAsync(t => t.Id == workOrder.TechnicianId.Value);

                if (!technicianExists)
                {
                    ModelState.AddModelError(
                        nameof(WorkOrder.TechnicianId),
                        "Select a valid technician.");
                }
            }

            if (workOrder.Status == WorkOrderStatuses.Completed &&
                string.IsNullOrWhiteSpace(workOrder.ResolutionNotes))
            {
                ModelState.AddModelError(
                    nameof(WorkOrder.ResolutionNotes),
                    "Enter resolution notes before completing the work order.");
            }

            if (!updated || !ModelState.IsValid)
            {
                await LoadTechnicians();

                return View("Edit", workOrder);
            }

            if (workOrder.Status == WorkOrderStatuses.Completed)
            {
                if (!workOrder.CompletedAt.HasValue)
                {
                    workOrder.CompletedAt = DateTime.UtcNow;
                }
            }
            else
            {
                workOrder.CompletedAt = null;
            }

            if (workOrder.SiteIssue != null)
            {
                if (workOrder.Status == WorkOrderStatuses.Pending)
                {
                    workOrder.SiteIssue.Status = SiteIssueStatuses.Open;
                }
                else if (workOrder.Status == WorkOrderStatuses.InProgress)
                {
                    workOrder.SiteIssue.Status = SiteIssueStatuses.InProgress;
                }
                else
                {
                    workOrder.SiteIssue.Status = SiteIssueStatuses.Completed;
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadSiteIssues(int? selectedId = null)
        {
            var assignedIds = await _context.WorkOrders
                .Select(w => w.SiteIssueId)
                .ToListAsync();

            var issues = await _context.SiteIssues
                .Where(i => !assignedIds.Contains(i.Id))
                .OrderBy(i => i.Title)
                .ToListAsync();

            ViewBag.SiteIssues = new SelectList(
                issues, "Id", "Title", selectedId);
        }

        private async Task LoadTechnicians()
        {
            var technicians = await _context.Technicians
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.Technicians = new SelectList(
                technicians, "Id", "Name");
        }
    }
}