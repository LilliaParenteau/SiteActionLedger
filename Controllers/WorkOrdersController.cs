
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
            bool requestExists = await _context.ServiceRequests
                .AnyAsync(r => r.Id == workOrder.ServiceRequestId);

            if (!requestExists)
            {
                ModelState.AddModelError("ServiceRequestId",
                    "Select a valid service request.");
            }
            else
            {
                bool alreadyAssigned = await _context.WorkOrders
                    .AnyAsync(w => w.ServiceRequestId == workOrder.ServiceRequestId);

                if (alreadyAssigned)
                {
                    ModelState.AddModelError("ServiceRequestId",
                        "This service request already has a work order.");
                }
            }

            if (ModelState.IsValid)
            {
                workOrder.Status = "Pending";
                workOrder.Notes ??= "";

                _context.WorkOrders.Add(workOrder);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadServiceRequests(workOrder.ServiceRequestId);
            return View(workOrder);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var workOrder = await _context.WorkOrders.FindAsync(id);

            if (workOrder == null)
            {
                return NotFound();
            }

            await LoadTechnicians(workOrder.TechnicianId);

            return View(workOrder);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id)
        {
            var workOrder = await _context.WorkOrders.FindAsync(id);

            if (workOrder == null)
            {
                return NotFound();
            }

            await TryUpdateModelAsync(workOrder, "",
                w => w.ScheduledDate,
                w => w.Status,
                w => w.Notes,
                w => w.TechnicianId);

            if (workOrder.Status != "Pending" &&
                workOrder.Status != "In Progress" &&
                workOrder.Status != "Completed")
            {
                ModelState.AddModelError("Status", "Select a valid status.");
            }

            if (workOrder.TechnicianId.HasValue)
            {
                bool technicianExists = await _context.Technicians
                    .AnyAsync(t => t.Id == workOrder.TechnicianId.Value);

                if (!technicianExists)
                {
                    ModelState.AddModelError("TechnicianId",
                        "Select a valid technician.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadTechnicians(workOrder.TechnicianId);
                return View("Edit", workOrder);
            }

            workOrder.Notes ??= "";

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = workOrder.Id });
        }

        private async Task LoadServiceRequests(int? selectedId = null)
        {
            var assignedIds = await _context.WorkOrders
                .Select(w => w.ServiceRequestId)
                .ToListAsync();

            var requests = await _context.ServiceRequests
                .Include(r => r.Customer)
                .OrderBy(r => r.Title)
                .ToListAsync();

            var availableRequests = requests
                .Where(r => !assignedIds.Contains(r.Id))
                .Select(r => new
                {
                    r.Id,
                    DisplayName = $"{r.Title} - {r.Customer?.Name}"
                })
                .ToList();

            ViewBag.ServiceRequests = new SelectList(
                availableRequests, "Id", "DisplayName", selectedId);
        }

        private async Task LoadTechnicians(int? selectedId = null)
        {
            var technicians = await _context.Technicians
                .OrderBy(t => t.Name)
                .Select(t => new
                {
                    t.Id,
                    DisplayName = $"{t.Name} - {t.Specialty}"
                })
                .ToListAsync();

            ViewBag.Technicians = new SelectList(
                technicians, "Id", "DisplayName", selectedId);
        }
    }
}
