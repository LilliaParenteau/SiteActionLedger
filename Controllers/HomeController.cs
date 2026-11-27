using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiteActionTrace.Data;
using SiteActionTrace.Models;

namespace SiteActionTrace.Controllers
{
    public class HomeController : Controller
    {
        private readonly SiteActionTraceContext _context;

        public HomeController(SiteActionTraceContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            DateTime today = DateTime.Today;

            var dashboard = new DashboardViewModel
            {
                CustomerCount = await _context.Customers.CountAsync(),

                TechnicianCount = await _context.Technicians.CountAsync(),

                ActiveProjectCount = await _context.Projects
                    .CountAsync(p => p.Status == ProjectStatuses.Active),

                OpenRequestCount = await _context.SiteIssues
                    .CountAsync(i => i.Status == SiteIssueStatuses.Open),

                UnresolvedIssueCount = await _context.SiteIssues
                    .CountAsync(i => i.Status != SiteIssueStatuses.Completed),

                ActiveWorkOrderCount = await _context.WorkOrders
                    .CountAsync(w => w.Status != WorkOrderStatuses.Completed),

                OverdueWorkOrderCount = await _context.WorkOrders
                    .CountAsync(w =>
                        w.ScheduledDate.HasValue &&
                        w.ScheduledDate.Value < today &&
                        w.Status != WorkOrderStatuses.Completed),

                RecentRequests = await _context.SiteIssues
                    .AsNoTracking()
                    .Include(i => i.Customer)
                    .Include(i => i.Project)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(5)
                    .ToListAsync(),

                UpcomingWorkOrders = await _context.WorkOrders
                    .AsNoTracking()
                    .Include(w => w.SiteIssue)
                    .Include(w => w.Technician)
                    .Where(w =>
                        w.ScheduledDate.HasValue &&
                        w.ScheduledDate.Value >= today &&
                        w.Status != WorkOrderStatuses.Completed)
                    .OrderBy(w => w.ScheduledDate)
                    .Take(5)
                    .ToListAsync(),

                OverdueWorkOrders = await _context.WorkOrders
                    .AsNoTracking()
                    .Include(w => w.SiteIssue)
                    .Include(w => w.Technician)
                    .Where(w =>
                        w.ScheduledDate.HasValue &&
                        w.ScheduledDate.Value < today &&
                        w.Status != WorkOrderStatuses.Completed)
                    .OrderBy(w => w.ScheduledDate)
                    .Take(5)
                    .ToListAsync(),

                ProjectIssueSummaries = await _context.Projects
                    .AsNoTracking()
                    .OrderBy(p => p.Name)
                    .Take(8)
                    .Select(p => new ProjectIssueSummary
                    {
                        ProjectId = p.Id,

                        ProjectName = p.Name,

                        CustomerName = p.Customer != null
                            ? p.Customer.Name
                            : "Unknown",

                        TotalIssueCount = _context.SiteIssues
                            .Count(i => i.ProjectId == p.Id),

                        UnresolvedIssueCount = _context.SiteIssues
                            .Count(i =>
                                i.ProjectId == p.Id &&
                                i.Status != SiteIssueStatuses.Completed)
                    })
                    .ToListAsync()
            };

            return View(dashboard);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}