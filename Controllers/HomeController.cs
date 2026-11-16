using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsPilot.Data;
using OpsPilot.Models;

namespace OpsPilot.Controllers
{
    public class HomeController : Controller
    {
        private readonly OpsPilotContext _context;

        public HomeController(OpsPilotContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = new DashboardViewModel
            {
                CustomerCount = await _context.Customers.CountAsync(),

                OpenRequestCount = await _context.ServiceRequests
                    .CountAsync(r => r.Status == "Open"),

                ActiveWorkOrderCount = await _context.WorkOrders
                    .CountAsync(w => w.Status != "Completed"),

                TechnicianCount = await _context.Technicians.CountAsync()
            };

            return View(dashboard);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
