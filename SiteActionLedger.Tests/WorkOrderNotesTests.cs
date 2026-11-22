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
    public class WorkOrderNotesTests
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

        private static async Task<ServiceRequest> AddIssue(
            SiteActionLedgerContext context)
        {
            var customer = new Customer
            {
                Name = "Harbor Construction",
                Email = "harbor@example.com",
                Phone = "321-555-0142",
                Address = "100 Main Street"
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var issue = new ServiceRequest
            {
                Title = "Electrical Inspection",
                Description = "Inspect exposed wiring.",
                CustomerId = customer.Id
            };

            context.ServiceRequests.Add(issue);
            await context.SaveChangesAsync();

            return issue;
        }

        [Fact]
        public async Task CannotCreateWorkOrderWithoutNotes()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);

            var issue = await AddIssue(context);

            var controller = new WorkOrdersController(context);

            var result = await controller.Create(new WorkOrder
            {
                ServiceRequestId = issue.Id,
                Notes = ""
            });

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Notes"));
            Assert.Equal(0, await context.WorkOrders.CountAsync());
        }

        [Fact]
        public async Task CannotEditWorkOrderWithEmptyNotes()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);

            var issue = await AddIssue(context);

            var order = new WorkOrder
            {
                ServiceRequestId = issue.Id,
                Status = "Pending",
                Notes = "Inspect the electrical wiring."
            };

            context.WorkOrders.Add(order);
            await context.SaveChangesAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddControllersWithViews();

            using var provider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = provider
            };

            httpContext.Request.Method = "POST";
            httpContext.Request.ContentType =
                "application/x-www-form-urlencoded";

            httpContext.Request.Form = new FormCollection(
                new Dictionary<string, StringValues>
                {
                    ["Status"] = "Pending",
                    ["Notes"] = "",
                    ["ResolutionNotes"] = ""
                });

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

            var result = await controller.EditPost(order.Id);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Notes"));

            await context.Entry(order).ReloadAsync();

            Assert.Equal(
                "Inspect the electrical wiring.",
                order.Notes);
        }
    }
}