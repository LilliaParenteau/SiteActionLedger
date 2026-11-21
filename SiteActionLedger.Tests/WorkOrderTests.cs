using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SiteActionLedger.Controllers;
using SiteActionLedger.Data;
using SiteActionLedger.Models;
using Xunit;

namespace SiteActionLedger.Tests
{
    public class WorkOrderTests
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

        [Fact]
        public async Task CannotDeleteAssignedTechnician()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var context = CreateContext(connection);

            var customer = new Customer
            {
                Name = "Harbor Construction",
                Email = "harbor@example.com",
                Address = "100 Main Street"
            };

            var technician = new Technician
            {
                Name = "Alex Morgan",
                Specialty = "Electrical"
            };

            context.Customers.Add(customer);
            context.Technicians.Add(technician);
            await context.SaveChangesAsync();

            var request = new ServiceRequest
            {
                Title = "Electrical Inspection",
                Description = "Inspect exposed wiring.",
                CustomerId = customer.Id
            };

            context.ServiceRequests.Add(request);
            await context.SaveChangesAsync();

            context.WorkOrders.Add(new WorkOrder
            {
                ServiceRequestId = request.Id,
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
                Name = "Jordan Lee",
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
                Name = "Coastal Builders",
                Email = "coastal@example.com",
                Address = "200 Main Street"
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var request = new ServiceRequest
            {
                Title = "Damaged Ceiling",
                Description = "Ceiling tiles need replacement.",
                CustomerId = customer.Id
            };

            context.ServiceRequests.Add(request);
            await context.SaveChangesAsync();

            context.WorkOrders.Add(new WorkOrder
            {
                ServiceRequestId = request.Id,
                Status = "Pending"
            });

            await context.SaveChangesAsync();

            var controller = new WorkOrdersController(context);

            var result = await controller.Create(new WorkOrder
            {
                ServiceRequestId = request.Id
            });

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);

            Assert.Equal(1, await context.WorkOrders
                .CountAsync(w => w.ServiceRequestId == request.Id));
        }

        [Fact]
        public async Task CannotAssignNonexistentTechnician()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var context = CreateContext(connection);

            var customer = new Customer
            {
                Name = "Westside Contractors",
                Email = "westside@example.com",
                Address = "300 Main Street"
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var request = new ServiceRequest
            {
                Title = "HVAC Repair",
                Description = "Inspect damaged ductwork.",
                CustomerId = customer.Id
            };

            context.ServiceRequests.Add(request);
            await context.SaveChangesAsync();

            var controller = new WorkOrdersController(context);

            var result = await controller.Create(new WorkOrder
            {
                ServiceRequestId = request.Id,
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
                Name = "Riverside Construction",
                Email = "riverside@example.com",
                Address = "400 Main Street"
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var request = new ServiceRequest
            {
                Title = "Water Leak",
                Description = "Leak found in mechanical room.",
                CustomerId = customer.Id,
                Status = "In Progress"
            };

            context.ServiceRequests.Add(request);
            await context.SaveChangesAsync();

            var controller = new WorkOrdersController(context);

            var result = await controller.Create(new WorkOrder
            {
                ServiceRequestId = request.Id,
                ScheduledDate = DateTime.Today.AddDays(2),
                Notes = "Inspect the water line.",
                Status = "Completed"
            });

            Assert.IsType<RedirectToActionResult>(result);

            var savedOrder = await context.WorkOrders
                .FirstOrDefaultAsync(w => w.ServiceRequestId == request.Id);

            Assert.NotNull(savedOrder);
            Assert.Equal("Pending", savedOrder.Status);

            var savedRequest = await context.ServiceRequests
                .FindAsync(request.Id);

            Assert.NotNull(savedRequest);
            Assert.Equal("Open", savedRequest.Status);
        }
    }
}