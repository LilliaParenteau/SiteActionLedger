using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpsPilot.Data;
using OpsPilot.Models;

namespace OpsPilot.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly OpsPilotContext _context;

        public ServiceRequestsController(OpsPilotContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var requests = await _context.ServiceRequests
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.ServiceRequests
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = new SelectList(
                await _context.Customers
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "Id", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title,Description,Priority,CustomerId")] ServiceRequest serviceRequest)
        {
            if (serviceRequest.Priority != "Low" &&
                serviceRequest.Priority != "Normal" &&
                serviceRequest.Priority != "High")
            {
                ModelState.AddModelError("Priority", "Select a valid priority.");
            }

            bool customerExists = await _context.Customers
                .AnyAsync(c => c.Id == serviceRequest.CustomerId);

            if (!customerExists)
            {
                ModelState.AddModelError("CustomerId", "Select a valid customer.");
            }

            if (ModelState.IsValid)
            {
                serviceRequest.Status = "Open";
                serviceRequest.CreatedAt = DateTime.UtcNow;

                _context.ServiceRequests.Add(serviceRequest);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Customers = new SelectList(
                await _context.Customers
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "Id", "Name", serviceRequest.CustomerId);

            return View(serviceRequest);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var request = await _context.ServiceRequests.FindAsync(id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id)
        {
            var request = await _context.ServiceRequests.FindAsync(id);

            if (request == null)
            {
                return NotFound();
            }

            await TryUpdateModelAsync(request, "",
                r => r.Title,
                r => r.Description,
                r => r.Priority,
                r => r.Status);

            if (request.Priority != "Low" &&
                request.Priority != "Normal" &&
                request.Priority != "High")
            {
                ModelState.AddModelError("Priority", "Select a valid priority.");
            }

            if (request.Status != "Open" &&
                request.Status != "In Progress" &&
                request.Status != "Completed")
            {
                ModelState.AddModelError("Status", "Select a valid status.");
            }

            if (!ModelState.IsValid)
            {
                return View("Edit", request);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = request.Id });
        }
    }
}
