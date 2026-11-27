using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiteActionTrace.Data;
using SiteActionTrace.Models;

namespace SiteActionTrace.Controllers
{
    public class CustomersController : Controller
    {
        private readonly SiteActionTraceContext _context;

        public CustomersController(SiteActionTraceContext context)
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

            ViewBag.SiteIssues = await _context.SiteIssues
                .Where(i => i.CustomerId == id)
                .OrderByDescending(i => i.CreatedAt)
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

            bool hasSiteIssues = await _context.SiteIssues
                .AnyAsync(i => i.CustomerId == id);

            bool hasProjects = await _context.Projects
                .AnyAsync(p => p.CustomerId == id);

            if (hasSiteIssues || hasProjects)
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