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
    public class SiteIssueTests
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

        private static async Task<Project> AddProject(
            SiteActionLedgerContext context)
        {
            var customer = new Customer
            {
                Name = "Harbor Construction",
                Email = "harbor@example.com",
                Address = "100 Main Street"
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var project = new Project
            {
                Name = "Office Renovation",
                SiteAddress = "200 Main Street",
                CustomerId = customer.Id
            };

            context.Projects.Add(project);
            await context.SaveChangesAsync();

            return project;
        }

        private static async Task<ServiceRequest> AddIssue(
            SiteActionLedgerContext context,
            Project project,
            string status = "Open")
        {
            var issue = new ServiceRequest
            {
                Title = "Electrical Inspection",
                Description = "Inspect exposed wiring.",
                Category = "Electrical",
                SiteArea = "Second-floor hallway",
                Priority = "High",
                Status = status,
                CustomerId = project.CustomerId,
                ProjectId = project.Id
            };

            context.ServiceRequests.Add(issue);
            await context.SaveChangesAsync();

            return issue;
        }

        private static ServiceProvider CreateServices()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddControllersWithViews();

            return services.BuildServiceProvider();
        }

        private static ServiceRequestsController CreateController(
            SiteActionLedgerContext context,
            IServiceProvider services,
            string action,
            Dictionary<string, StringValues>? formValues = null)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = services
            };

            httpContext.Request.Method = "POST";
            httpContext.Request.ContentType =
                "application/x-www-form-urlencoded";

            httpContext.Request.Form = new FormCollection(
                formValues ?? new Dictionary<string, StringValues>());

            var routeData = new RouteData();
            routeData.Values["controller"] = "ServiceRequests";
            routeData.Values["action"] = action;

            return new ServiceRequestsController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext,
                    RouteData = routeData,
                    ActionDescriptor = new ControllerActionDescriptor
                    {
                        ControllerName = "ServiceRequests",
                        ActionName = action
                    },
                    ValueProviderFactories =
                        new List<IValueProviderFactory>
                        {
                            new FormValueProviderFactory()
                        }
                }
            };
        }

        private static Dictionary<string, StringValues> IssueForm(
            Project project,
            string title = "Updated Electrical Issue")
        {
            return new Dictionary<string, StringValues>
            {
                ["Title"] = title,
                ["Description"] = "Inspect and repair exposed wiring.",
                ["Category"] = "Electrical",
                ["SiteArea"] = "Second-floor mechanical room",
                ["Priority"] = "High",
                ["ProjectId"] = project.Id.ToString()
            };
        }

        [Fact]
        public async Task CreatingIssueUsesProjectCustomerAndOpenStatus()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);
            using var services = CreateServices();

            var project = await AddProject(context);

            var controller = CreateController(
                context, services, "Create");

            var issue = new ServiceRequest
            {
                Title = "Exposed Wiring",
                Description = "Wiring requires inspection.",
                Category = "Electrical",
                SiteArea = "Mechanical room",
                Priority = "High",
                ProjectId = project.Id,
                Status = "Completed"
            };

            var result = await controller.Create(issue);

            Assert.IsType<RedirectToActionResult>(result);

            var savedIssue = await context.ServiceRequests
                .SingleAsync();

            Assert.Equal(project.Id, savedIssue.ProjectId);
            Assert.Equal(project.CustomerId, savedIssue.CustomerId);
            Assert.Equal("Open", savedIssue.Status);
            Assert.Equal("Electrical", savedIssue.Category);
            Assert.Equal("Mechanical room", savedIssue.SiteArea);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(9999)]
        public async Task CannotCreateIssueWithInvalidProject(
            int? projectId)
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);
            using var services = CreateServices();

            await AddProject(context);

            var controller = CreateController(
                context, services, "Create");

            var issue = new ServiceRequest
            {
                Title = "Site Inspection",
                Description = "Inspection required.",
                ProjectId = projectId
            };

            var result = await controller.Create(issue);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal(0, await context.ServiceRequests.CountAsync());
        }

        [Fact]
        public async Task CannotCreateIssueWithInvalidCategory()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);
            using var services = CreateServices();

            var project = await AddProject(context);

            var controller = CreateController(
                context, services, "Create");

            var issue = new ServiceRequest
            {
                Title = "Electrical Inspection",
                Description = "Inspect the wiring.",
                Category = "Invalid Category",
                ProjectId = project.Id
            };

            var result = await controller.Create(issue);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal(0, await context.ServiceRequests.CountAsync());
        }

        [Fact]
        public async Task CannotCreateIssueWithInvalidPriority()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);
            using var services = CreateServices();

            var project = await AddProject(context);

            var controller = CreateController(
                context, services, "Create");

            var issue = new ServiceRequest
            {
                Title = "Plumbing Inspection",
                Description = "Inspect water connections.",
                Category = "Plumbing",
                Priority = "Critical",
                ProjectId = project.Id
            };

            var result = await controller.Create(issue);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal(0, await context.ServiceRequests.CountAsync());
        }

        [Fact]
        public async Task LinkedWorkOrderControlsIssueStatus()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);
            using var services = CreateServices();

            var project = await AddProject(context);

            var issue = await AddIssue(
                context, project, "In Progress");

            context.WorkOrders.Add(new WorkOrder
            {
                ServiceRequestId = issue.Id,
                Status = "In Progress",
                Notes = "Repairs underway."
            });

            await context.SaveChangesAsync();

            var formValues = IssueForm(project);
            formValues["Status"] = "Completed";

            var controller = CreateController(
                context, services, "Edit", formValues);

            var result = await controller.EditPost(
                issue.Id, "Completed");

            Assert.IsType<RedirectToActionResult>(result);

            await context.Entry(issue).ReloadAsync();

            Assert.Equal("In Progress", issue.Status);
            Assert.Equal("Updated Electrical Issue", issue.Title);

            var order = await context.WorkOrders.SingleAsync();

            Assert.Equal("In Progress", order.Status);
        }

        [Fact]
        public async Task IssueWithoutWorkOrderCanChangeStatus()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);
            using var services = CreateServices();

            var project = await AddProject(context);

            var issue = await AddIssue(context, project);

            var controller = CreateController(
                context,
                services,
                "Edit",
                IssueForm(project));

            var result = await controller.EditPost(
                issue.Id, "In Progress");

            Assert.IsType<RedirectToActionResult>(result);

            await context.Entry(issue).ReloadAsync();

            Assert.Equal("In Progress", issue.Status);
            Assert.Equal("Updated Electrical Issue", issue.Title);
        }
    }
}