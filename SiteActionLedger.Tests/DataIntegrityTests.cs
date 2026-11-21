using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SiteActionLedger.Controllers;
using SiteActionLedger.Data;
using SiteActionLedger.Models;
using Xunit;

namespace SiteActionLedger.Tests
{
    public class DataIntegrityTests
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
        public async Task CannotDeleteCustomerWithProjects()
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

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            context.Projects.Add(new Project
            {
                Name = "Office Renovation",
                SiteAddress = "200 Main Street",
                CustomerId = customer.Id
            });

            await context.SaveChangesAsync();

            var controller = new CustomersController(context);

            var result = await controller.DeleteConfirmed(customer.Id);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);

            Assert.True(await context.Customers
                .AnyAsync(c => c.Id == customer.Id));
        }

        [Fact]
        public async Task CanDeleteCustomerWithoutProjectsOrIssues()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var context = CreateContext(connection);

            var customer = new Customer
            {
                Name = "Test Customer",
                Email = "test@example.com",
                Address = "300 Main Street"
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var controller = new CustomersController(context);

            var result = await controller.DeleteConfirmed(customer.Id);

            Assert.IsType<RedirectToActionResult>(result);

            Assert.False(await context.Customers
                .AnyAsync(c => c.Id == customer.Id));
        }

        [Fact]
        public async Task CannotChangeProjectClientWhenIssuesExist()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var context = CreateContext(connection);

            var originalClient = new Customer
            {
                Name = "Original Client",
                Email = "original@example.com",
                Address = "100 First Street"
            };

            var newClient = new Customer
            {
                Name = "New Client",
                Email = "new@example.com",
                Address = "200 Second Street"
            };

            context.Customers.AddRange(originalClient, newClient);
            await context.SaveChangesAsync();

            var project = new Project
            {
                Name = "Office Construction",
                SiteAddress = "500 Project Road",
                CustomerId = originalClient.Id
            };

            context.Projects.Add(project);
            await context.SaveChangesAsync();

            context.ServiceRequests.Add(new ServiceRequest
            {
                Title = "Electrical Issue",
                Description = "Wiring needs inspection.",
                CustomerId = originalClient.Id,
                ProjectId = project.Id,
                Category = "Electrical"
            });

            await context.SaveChangesAsync();

            var controller = new ProjectsController(context);

            var result = await controller.EditPost(
                project.Id,
                new Project
                {
                    Id = project.Id,
                    Name = project.Name,
                    SiteAddress = project.SiteAddress,
                    CustomerId = newClient.Id,
                    Status = "Active"
                });

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);

            var savedProject = await context.Projects.FindAsync(project.Id);

            Assert.Equal(originalClient.Id, savedProject!.CustomerId);
        }

        [Fact]
        public async Task CanChangeProjectClientWhenNoIssuesExist()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using var context = CreateContext(connection);

            var originalClient = new Customer
            {
                Name = "Original Client",
                Email = "original@example.com",
                Address = "100 First Street"
            };

            var newClient = new Customer
            {
                Name = "New Client",
                Email = "new@example.com",
                Address = "200 Second Street"
            };

            context.Customers.AddRange(originalClient, newClient);
            await context.SaveChangesAsync();

            var project = new Project
            {
                Name = "New Construction",
                SiteAddress = "600 Project Road",
                CustomerId = originalClient.Id
            };

            context.Projects.Add(project);
            await context.SaveChangesAsync();

            var controller = new ProjectsController(context);

            var result = await controller.EditPost(
                project.Id,
                new Project
                {
                    Id = project.Id,
                    Name = project.Name,
                    SiteAddress = project.SiteAddress,
                    CustomerId = newClient.Id,
                    Status = "Active"
                });

            Assert.IsType<RedirectToActionResult>(result);

            var savedProject = await context.Projects.FindAsync(project.Id);

            Assert.Equal(newClient.Id, savedProject!.CustomerId);
        }
    }
}