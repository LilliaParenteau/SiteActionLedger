using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiteActionLedger.Data;
using SiteActionLedger.Models;

namespace SiteActionLedger.Controllers
{
    public class CustomersController : Controller
    {
        private readonly SiteActionLedgerContext _context;

        public CustomersController(SiteActionLedgerContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var customers = _context.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                customers = customers.Where(c =>
                    c.Name.Contains(search) ||
                    c.Email.Contains(search) ||
                    c.Phone.Contains(search) ||
                    c.Address.Contains(search));
            }

            ViewBag.Search = search;

            return View(await customers
                .OrderBy(c => c.Name)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            ViewBag.ServiceRequests = await _context.ServiceRequests
                .Where(r => r.CustomerId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(customer);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Email,Phone,Address")] Customer customer)
        {
            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                customer.Phone = "";
                ModelState.Remove(nameof(Customer.Phone));
            }

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id)
        {
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            bool updated = await TryUpdateModelAsync(
                customer,
                "",
                c => c.Name,
                c => c.Email,
                c => c.Phone,
                c => c.Address
            );

            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                customer.Phone = "";
                ModelState.Remove(nameof(Customer.Phone));
            }

            if (!updated || !ModelState.IsValid)
            {
                return View("Edit", customer);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            bool hasRequests = await _context.ServiceRequests
                .AnyAsync(r => r.CustomerId == id);

            bool hasProjects = await _context.Projects
                .AnyAsync(p => p.CustomerId == id);

            if (hasRequests || hasProjects)
            {
                ModelState.AddModelError(
                    "",
                    "This customer has existing projects or site issues and cannot be deleted.");

                return View("Delete", customer);
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}