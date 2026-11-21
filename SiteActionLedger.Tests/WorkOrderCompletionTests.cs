using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using SiteActionLedger.Controllers;
using SiteActionLedger.Data;
using SiteActionLedger.Models;
using Xunit;

namespace SiteActionLedger.Tests
{
    public class WorkOrderCompletionTests
    {
        private static SiteActionLedgerContext CreateContext(
            SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<SiteActionLedgerContext>()
                .UseSqlite(connection)
                .Options;

            var context = new SiteActionLedgerContext(options);

            context.Database.EnsureCreated();

            return context;
        }

        private static async Task<(int OrderId, int IssueId)> AddWorkOrder(
            SiteActionLedgerContext context,
            string status,
            string? resolutionNotes = null,
            DateTime? completedAt = null)
        {
            var customer = new Customer
            {
                Name = "Harbor Construction",
                Email = "harbor@example.com",
                Address = "100 Main Street"
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var issue = new ServiceRequest
            {
                Title = "Exposed Wiring",
                Description = "Electrical wiring requires inspection.",
                CustomerId = customer.Id,
                Status = status == "Pending" ? "Open" : status
            };

            context.ServiceRequests.Add(issue);
            await context.SaveChangesAsync();

            var order = new WorkOrder
            {
                ServiceRequestId = issue.Id,
                Status = status,
                Notes = "Inspect the wiring.",
                ResolutionNotes = resolutionNotes,
                CompletedAt = completedAt
            };

            context.WorkOrders.Add(order);
            await context.SaveChangesAsync();

            return (order.Id, issue.Id);
        }

        private static WorkOrdersController CreateController(
            SiteActionLedgerContext context,
            IServiceProvider services,
            string status,
            string? resolutionNotes)
        {
            var formValues = new Dictionary<string, StringValues>
            {
                ["Status"] = status,
                ["Notes"] = "Inspect and repair the wiring.",
                ["ResolutionNotes"] = resolutionNotes ?? ""
            };

            var httpContext = new DefaultHttpContext
            {
                RequestServices = services
            };

            httpContext.Request.Method = "POST";
            httpContext.Request.ContentType =
                "application/x-www-form-urlencoded";

            httpContext.Request.Form = new FormCollection(formValues);

            var routeData = new RouteData();
            routeData.Values["controller"] = "WorkOrders";
            routeData.Values["action"] = "Edit";

            var controller = new WorkOrdersController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext,
                    RouteData = routeData,
                    ActionDescriptor = new ControllerActionDescriptor
                    {
                        ControllerName = "WorkOrders",
                        ActionName = "Edit"
                    },
                    ValueProviderFactories =
                        new List<IValueProviderFactory>
                        {
                            new FormValueProviderFactory()
                        }
                }
            };

            return controller;
        }

        private static ServiceProvider CreateServices()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddControllersWithViews();

            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task CannotCompleteWithoutResolutionNotes()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);
            using var services = CreateServices();

            var ids = await AddWorkOrder(context, "Pending");

            var controller = CreateController(
                context,
                services,
                "Completed",
                "");

            var result = await controller.EditPost(ids.OrderId);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);

            var order = await context.WorkOrders.FindAsync(ids.OrderId);
            await context.Entry(order!).ReloadAsync();

            Assert.Equal("Pending", order!.Status);
            Assert.Null(order.CompletedAt);

            var issue = await context.ServiceRequests.FindAsync(ids.IssueId);
            await context.Entry(issue!).ReloadAsync();

            Assert.Equal("Open", issue!.Status);
        }

        [Fact]
        public async Task CompletingOrderRecordsDateAndClosesIssue()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);
            using var services = CreateServices();

            var ids = await AddWorkOrder(context, "In Progress");

            var controller = CreateController(
                context,
                services,
                "Completed",
                "Repaired and inspected the electrical wiring.");

            DateTime beforeSave = DateTime.UtcNow;

            var result = await controller.EditPost(ids.OrderId);

            Assert.IsType<RedirectToActionResult>(result);

            var order = await context.WorkOrders.FindAsync(ids.OrderId);

            Assert.NotNull(order);
            Assert.Equal("Completed", order.Status);

            Assert.Equal(
                "Repaired and inspected the electrical wiring.",
                order.ResolutionNotes);

            Assert.NotNull(order.CompletedAt);

            Assert.InRange(
                order.CompletedAt.Value,
                beforeSave,
                DateTime.UtcNow.AddSeconds(5));

            var issue = await context.ServiceRequests.FindAsync(ids.IssueId);

            Assert.NotNull(issue);
            Assert.Equal("Completed", issue.Status);
        }

        [Fact]
        public async Task EditingCompletedOrderPreservesCompletionDate()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);
            using var services = CreateServices();

            DateTime originalDate = DateTime.UtcNow.AddDays(-2);

            var ids = await AddWorkOrder(
                context,
                "Completed",
                "Original repair completed.",
                originalDate);

            var controller = CreateController(
                context,
                services,
                "Completed",
                "Updated repair notes.");

            var result = await controller.EditPost(ids.OrderId);

            Assert.IsType<RedirectToActionResult>(result);

            var order = await context.WorkOrders.FindAsync(ids.OrderId);

            Assert.NotNull(order);
            Assert.Equal("Completed", order.Status);
            Assert.Equal(originalDate, order.CompletedAt);
            Assert.Equal("Updated repair notes.", order.ResolutionNotes);
        }

        [Fact]
        public async Task ReopeningOrderClearsCompletionDate()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);
            using var services = CreateServices();

            var ids = await AddWorkOrder(
                context,
                "Completed",
                "Initial repairs completed.",
                DateTime.UtcNow.AddDays(-1));

            var controller = CreateController(
                context,
                services,
                "In Progress",
                "Additional repairs required.");

            var result = await controller.EditPost(ids.OrderId);

            Assert.IsType<RedirectToActionResult>(result);

            var order = await context.WorkOrders.FindAsync(ids.OrderId);

            Assert.NotNull(order);
            Assert.Equal("In Progress", order.Status);
            Assert.Null(order.CompletedAt);

            var issue = await context.ServiceRequests.FindAsync(ids.IssueId);

            Assert.NotNull(issue);
            Assert.Equal("In Progress", issue.Status);
        }
    }
}