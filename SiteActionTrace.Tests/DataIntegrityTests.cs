using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SiteActionTrace.Controllers;
using SiteActionTrace.Data;
using SiteActionTrace.Models;
using Xunit;

namespace SiteActionTrace.Tests
{
    public class DataIntegrityTests
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
        public async Task CannotDeleteCustomerWithProjects()
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

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            context.Projects.Add(new Project
            {
                Name = "Office Renovation",
                SiteAddress = "Demo Address 02",
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
                Address = "Demo Address 03"
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
                Address = "Demo Address 05"
            };

            var newClient = new Customer
            {
                Name = "New Client",
                Email = "new@example.com",
                Address = "Demo Address 06"
            };

            context.Customers.AddRange(originalClient, newClient);
            await context.SaveChangesAsync();

            var project = new Project
            {
                Name = "Office Construction",
                SiteAddress = "Demo Site 05",
                CustomerId = originalClient.Id
            };

            context.Projects.Add(project);
            await context.SaveChangesAsync();

            context.SiteIssues.Add(new SiteIssue
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
                Address = "Demo Address 05"
            };

            var newClient = new Customer
            {
                Name = "New Client",
                Email = "new@example.com",
                Address = "Demo Address 06"
            };

            context.Customers.AddRange(originalClient, newClient);
            await context.SaveChangesAsync();

            var project = new Project
            {
                Name = "New Construction",
                SiteAddress = "Demo Site 06",
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