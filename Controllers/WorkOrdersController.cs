using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpsPilot.Data;
using OpsPilot.Models;

namespace OpsPilot.Controllers
{
    public class WorkOrdersController : Controller
    {
        private readonly OpsPilotContext _context;

        public WorkOrdersController(OpsPilotContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var workOrders = await _context.WorkOrders
                .Include(w => w.ServiceRequest)
                .ThenInclude(r => r!.Customer)
                .Include(w => w.Technician)
                .OrderByDescending(w => w.Id)
                .ToListAsync();

            return View(workOrders);
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

        public async Task<IActionResult> Create()
        {
            await LoadServiceRequests();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ServiceRequestId,ScheduledDate,Notes")] WorkOrder workOrder)
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
                .AnyAsync(w => w.ServiceRequestId == workOrder.ServiceRequestId);

            if (alreadyAssigned)
            {
                ModelState.AddModelError(
                    "ServiceRequestId",
                    "This request already has a work order.");
            }

            if (!ModelState.IsValid)
            {
                await LoadServiceRequests();
                return View(workOrder);
            }

            workOrder.Status = "Pending";
            workOrder.Notes ??= "";

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
                .Include(w => w.Technician)
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
                w => w.TechnicianId
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
                    .AnyAsync(t => t.Id == workOrder.TechnicianId.Value);

                if (!technicianExists)
                {
                    ModelState.AddModelError(
                        "TechnicianId",
                        "Select a valid technician.");
                }
            }

            if (!updated || !ModelState.IsValid)
            {
                await LoadTechnicians();
                return View("Edit", workOrder);
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

        private async Task LoadServiceRequests()
        {
            var assignedIds = await _context.WorkOrders
                .Select(w => w.ServiceRequestId)
                .ToListAsync();

            var requests = await _context.ServiceRequests
                .Where(r => !assignedIds.Contains(r.Id))
                .OrderBy(r => r.Title)
                .ToListAsync();

            ViewBag.ServiceRequests = new SelectList(
                requests, "Id", "Title");
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
