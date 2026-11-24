using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SiteActionLedger.Data;
using SiteActionLedger.Models;

namespace SiteActionLedger.Controllers
{
    public class WorkOrdersController : Controller
    {
        private readonly SiteActionLedgerContext _context;

        public WorkOrdersController(SiteActionLedgerContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            var workOrders = _context.WorkOrders
                .Include(w => w.ServiceRequest)
                .ThenInclude(r => r!.Customer)
                .Include(w => w.Technician)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                workOrders = workOrders.Where(w =>
                    w.ServiceRequest!.Title.Contains(search) ||
                    w.ServiceRequest.Customer!.Name.Contains(search) ||
                    (w.Technician != null &&
                     w.Technician.Name.Contains(search)));
            }

            if (status == "Pending" ||
                status == "In Progress" ||
                status == "Completed")
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
                .Include(w => w.ServiceRequest)
                .ThenInclude(r => r!.Customer)
                .Include(w => w.Technician)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workOrder == null)
            {
                return NotFound();
            }

            return View(workOrder);
        }

        public async Task<IActionResult> Create(int? serviceRequestId)
        {
            await LoadServiceRequests(serviceRequestId);
            await LoadTechnicians();

            var workOrder = new WorkOrder
            {
                ServiceRequestId = serviceRequestId ?? 0
            };

            return View(workOrder);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ServiceRequestId,ScheduledDate,Notes,TechnicianId")]
            WorkOrder workOrder)
        {
            var request = await _context.ServiceRequests
                .FindAsync(workOrder.ServiceRequestId);

            if (request == null)
            {
                ModelState.AddModelError(
                    "ServiceRequestId",
                    "Select a valid service request.");
            }

            bool alreadyAssigned = await _context.WorkOrders
                .AnyAsync(w =>
                    w.ServiceRequestId == workOrder.ServiceRequestId);

            if (alreadyAssigned)
            {
                ModelState.AddModelError(
                    "ServiceRequestId",
                    "This request already has a work order.");
            }

            if (workOrder.TechnicianId.HasValue)
            {
                bool technicianExists = await _context.Technicians
                    .AnyAsync(t =>
                        t.Id == workOrder.TechnicianId.Value);

                if (!technicianExists)
                {
                    ModelState.AddModelError(
                        "TechnicianId",
                        "Select a valid technician.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadServiceRequests(workOrder.ServiceRequestId);
                await LoadTechnicians();

                return View(workOrder);
            }

            workOrder.Status = "Pending";
            request!.Status = "Open";

            _context.WorkOrders.Add(workOrder);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var workOrder = await _context.WorkOrders
                .Include(w => w.ServiceRequest)
                .ThenInclude(r => r!.Customer)
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
                .Include(w => w.ServiceRequest)
                .ThenInclude(r => r!.Customer)
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

            if (workOrder.Status != "Pending" &&
                workOrder.Status != "In Progress" &&
                workOrder.Status != "Completed")
            {
                ModelState.AddModelError(
                    "Status",
                    "Select a valid status.");
            }

            if (workOrder.TechnicianId.HasValue)
            {
                bool technicianExists = await _context.Technicians
                    .AnyAsync(t =>
                        t.Id == workOrder.TechnicianId.Value);

                if (!technicianExists)
                {
                    ModelState.AddModelError(
                        "TechnicianId",
                        "Select a valid technician.");
                }
            }

            if (workOrder.Status == "Completed" &&
                string.IsNullOrWhiteSpace(workOrder.ResolutionNotes))
            {
                ModelState.AddModelError(
                    "ResolutionNotes",
                    "Enter resolution notes before completing the work order.");
            }

            if (!updated || !ModelState.IsValid)
            {
                await LoadTechnicians();

                return View("Edit", workOrder);
            }

            if (workOrder.Status == "Completed")
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

            if (workOrder.ServiceRequest != null)
            {
                if (workOrder.Status == "Pending")
                {
                    workOrder.ServiceRequest.Status = "Open";
                }
                else if (workOrder.Status == "In Progress")
                {
                    workOrder.ServiceRequest.Status = "In Progress";
                }
                else if (workOrder.Status == "Completed")
                {
                    workOrder.ServiceRequest.Status = "Completed";
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadServiceRequests(int? selectedId = null)
        {
            var assignedIds = await _context.WorkOrders
                .Select(w => w.ServiceRequestId)
                .ToListAsync();

            var requests = await _context.ServiceRequests
                .Where(r => !assignedIds.Contains(r.Id))
                .OrderBy(r => r.Title)
                .ToListAsync();

            ViewBag.ServiceRequests = new SelectList(
                requests, "Id", "Title", selectedId);
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