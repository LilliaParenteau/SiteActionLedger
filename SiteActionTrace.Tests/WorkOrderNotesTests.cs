using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using SiteActionTrace.Controllers;
using SiteActionTrace.Data;
using SiteActionTrace.Models;
using Xunit;

namespace SiteActionTrace.Tests
{
    public class WorkOrderNotesTests
    {
        private static SiteActionTraceContext CreateContext(
            SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<SiteActionTraceContext>()
                .UseSqlite(connection)
                .Options;

            var context = new SiteActionTraceContext(options);
            context.Database.EnsureCreated();

            return context;
        }

        private static async Task<SiteIssue> AddIssue(
            SiteActionTraceContext context)
        {
            var customer = new Customer
            {
                Name = "Demo Client A",
                Email = "harbor@example.com",
                Phone = "321-555-0142",
                Address = "Demo Address 01"
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var issue = new SiteIssue
            {
                Title = "Electrical Inspection",
                Description = "Inspect exposed wiring.",
                CustomerId = customer.Id
            };

            context.SiteIssues.Add(issue);
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

            var workOrder = new WorkOrder
            {
                SiteIssueId = issue.Id,
                Notes = ""
            };

            var validationResults = new List<ValidationResult>();

            bool valid = Validator.TryValidateObject(
                workOrder,
                new ValidationContext(workOrder),
                validationResults,
                validateAllProperties: true);

            Assert.False(valid);

            Assert.Contains(validationResults, result =>
                result.MemberNames.Contains(nameof(WorkOrder.Notes)));

            // Simulate the model validation performed by MVC.
            var controller = new WorkOrdersController(context);

            foreach (var validationResult in validationResults)
            {
                foreach (var member in validationResult.MemberNames)
                {
                    controller.ModelState.AddModelError(
                        member,
                        validationResult.ErrorMessage ?? "Invalid value.");
                }
            }

            var actionResult = await controller.Create(workOrder);

            Assert.IsType<ViewResult>(actionResult);
            Assert.False(controller.ModelState.IsValid);
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
                SiteIssueId = issue.Id,
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