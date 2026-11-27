using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SiteActionTrace.Controllers;
using SiteActionTrace.Data;
using SiteActionTrace.Models;
using Xunit;

namespace SiteActionTrace.Tests
{
    public class WorkOrderTests
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

        [Fact]
        public async Task CannotDeleteAssignedTechnician()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var context = CreateContext(connection);

            var customer = new Customer
            {
                Name = "Demo Client A",
                Email = "harbor@example.com",
                Address = "Demo Address 01"
            };

            var technician = new Technician
            {
                Name = "Demo Technician A",
                Specialty = "Electrical"
            };

            context.Customers.Add(customer);
            context.Technicians.Add(technician);
            await context.SaveChangesAsync();

            var issue = new SiteIssue
            {
                Title = "Electrical Inspection",
                Description = "Inspect exposed wiring.",
                CustomerId = customer.Id
            };

            context.SiteIssues.Add(issue);
            await context.SaveChangesAsync();

            context.WorkOrders.Add(new WorkOrder
            {
                SiteIssueId = issue.Id,
                TechnicianId = technician.Id,
                Status = "Pending"
            });

            await context.SaveChangesAsync();

            var controller = new TechniciansController(context);

            var result = await controller.DeleteConfirmed(technician.Id);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);

            Assert.True(await context.Technicians
                .AnyAsync(t => t.Id == technician.Id));
        }

        [Fact]
        public async Task CanDeleteUnassignedTechnician()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var context = CreateContext(connection);

            var technician = new Technician
            {
                Name = "Demo Technician B",
                Specialty = "Plumbing"
            };

            context.Technicians.Add(technician);
            await context.SaveChangesAsync();

            var controller = new TechniciansController(context);

            var result = await controller.DeleteConfirmed(technician.Id);

            Assert.IsType<RedirectToActionResult>(result);

            Assert.False(await context.Technicians
                .AnyAsync(t => t.Id == technician.Id));
        }

        [Fact]
        public async Task CannotCreateDuplicateWorkOrder()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var context = CreateContext(connection);

            var customer = new Customer
            {
                Name = "Demo Client B",
                Email = "coastal@example.com",
                Address = "Demo Address 02"
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var issue = new SiteIssue
            {
                Title = "Damaged Ceiling",
                Description = "Ceiling tiles need replacement.",
                CustomerId = customer.Id
            };

            context.SiteIssues.Add(issue);
            await context.SaveChangesAsync();

            context.WorkOrders.Add(new WorkOrder
            {
                SiteIssueId = issue.Id,
                Status = "Pending"
            });

            await context.SaveChangesAsync();

            var controller = new WorkOrdersController(context);

            var result = await controller.Create(new WorkOrder
            {
                SiteIssueId = issue.Id
            });

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);

            Assert.Equal(1, await context.WorkOrders
                .CountAsync(w => w.SiteIssueId == issue.Id));
        }

        [Fact]
        public async Task CannotAssignNonexistentTechnician()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var context = CreateContext(connection);

            var customer = new Customer
            {
                Name = "Demo Client C",
                Email = "westside@example.com",
                Address = "Demo Address 03"
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var issue = new SiteIssue
            {
                Title = "HVAC Repair",
                Description = "Inspect damaged ductwork.",
                CustomerId = customer.Id
            };

            context.SiteIssues.Add(issue);
            await context.SaveChangesAsync();

            var controller = new WorkOrdersController(context);

            var result = await controller.Create(new WorkOrder
            {
                SiteIssueId = issue.Id,
                TechnicianId = 9999
            });

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);

            Assert.Equal(0, await context.WorkOrders.CountAsync());
        }

        [Fact]
        public async Task NewWorkOrderStartsPendingAndOpensIssue()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var context = CreateContext(connection);

            var customer = new Customer
            {
                Name = "Demo Client D",
                Email = "riverside@example.com",
                Address = "Demo Address 04"
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var issue = new SiteIssue
            {
                Title = "Water Leak",
                Description = "Leak found in mechanical room.",
                CustomerId = customer.Id,
                Status = "In Progress"
            };

            context.SiteIssues.Add(issue);
            await context.SaveChangesAsync();

            var controller = new WorkOrdersController(context);

            var result = await controller.Create(new WorkOrder
            {
                SiteIssueId = issue.Id,
                ScheduledDate = DateTime.Today.AddDays(2),
                Notes = "Inspect the water line.",
                Status = "Completed"
            });

            Assert.IsType<RedirectToActionResult>(result);

            var savedOrder = await context.WorkOrders
                .FirstOrDefaultAsync(w => w.SiteIssueId == issue.Id);

            Assert.NotNull(savedOrder);
            Assert.Equal("Pending", savedOrder.Status);

            var savedIssue = await context.SiteIssues
                .FindAsync(issue.Id);

            Assert.NotNull(savedIssue);
            Assert.Equal("Open", savedIssue.Status);
        }
    }
}