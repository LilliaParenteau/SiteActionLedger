using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SiteActionTrace.Data;
using SiteActionTrace.Models;
using Xunit;

namespace SiteActionTrace.Tests
{
    public class DemoDataTests
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
        public async Task InitializesEmptyDatabase()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);

            Assert.True(await DemoData.InitializeAsync(context));

            context.ChangeTracker.Clear();

            Assert.Equal(2, await context.Customers.CountAsync());
            Assert.Equal(3, await context.Projects.CountAsync());
            Assert.Equal(3, await context.Technicians.CountAsync());
            Assert.Equal(4, await context.SiteIssues.CountAsync());
            Assert.Equal(3, await context.WorkOrders.CountAsync());

            var completedOrder = await context.WorkOrders
                .Include(w => w.SiteIssue)
                .SingleAsync(w =>
                    w.Status == WorkOrderStatuses.Completed);

            Assert.NotNull(completedOrder.CompletedAt);
            Assert.False(string.IsNullOrWhiteSpace(
                completedOrder.ResolutionNotes));

            Assert.NotNull(completedOrder.SiteIssue);
            Assert.Equal(SiteIssueStatuses.Completed,
                completedOrder.SiteIssue.Status);
        }

        [Fact]
        public async Task DoesNotDuplicateDemonstrationData()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);

            Assert.True(await DemoData.InitializeAsync(context));
            Assert.False(await DemoData.InitializeAsync(context));

            Assert.Equal(2, await context.Customers.CountAsync());
            Assert.Equal(3, await context.Projects.CountAsync());
            Assert.Equal(3, await context.Technicians.CountAsync());
            Assert.Equal(4, await context.SiteIssues.CountAsync());
            Assert.Equal(3, await context.WorkOrders.CountAsync());
        }

        [Fact]
        public async Task PreservesExistingCustomer()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);

            context.Customers.Add(new Customer
            {
                Name = "Demo Existing Client",
                Email = "existing@example.com",
                Phone = "321-555-0110",
                Address = "Demo Address 01"
            });

            await context.SaveChangesAsync();

            Assert.False(await DemoData.InitializeAsync(context));

            context.ChangeTracker.Clear();

            var customer = await context.Customers.SingleAsync();

            Assert.Equal("Demo Existing Client", customer.Name);
            Assert.Equal("existing@example.com", customer.Email);
            Assert.False(await context.Projects.AnyAsync());
            Assert.False(await context.Technicians.AnyAsync());
            Assert.False(await context.SiteIssues.AnyAsync());
            Assert.False(await context.WorkOrders.AnyAsync());
        }

        [Fact]
        public async Task PreservesExistingTechnician()
        {
            using var connection =
                new SqliteConnection("Data Source=:memory:");

            connection.Open();

            using var context = CreateContext(connection);

            context.Technicians.Add(new Technician
            {
                Name = "Demo Existing Technician",
                Specialty = "Electrical",
                Phone = "321-555-0111"
            });

            await context.SaveChangesAsync();

            Assert.False(await DemoData.InitializeAsync(context));

            context.ChangeTracker.Clear();

            var technician = await context.Technicians.SingleAsync();

            Assert.Equal("Demo Existing Technician", technician.Name);
            Assert.Equal("Electrical", technician.Specialty);
            Assert.False(await context.Customers.AnyAsync());
            Assert.False(await context.Projects.AnyAsync());
            Assert.False(await context.SiteIssues.AnyAsync());
            Assert.False(await context.WorkOrders.AnyAsync());
        }
    }
}