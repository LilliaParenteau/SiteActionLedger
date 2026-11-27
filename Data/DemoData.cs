using Microsoft.EntityFrameworkCore;
using SiteActionTrace.Models;

namespace SiteActionTrace.Data
{
    public static class DemoData
    {
        public static async Task<bool> InitializeAsync(
            SiteActionTraceContext context)
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync();

            if (await context.Customers.AnyAsync() ||
                await context.Projects.AnyAsync() ||
                await context.Technicians.AnyAsync() ||
                await context.SiteIssues.AnyAsync() ||
                await context.WorkOrders.AnyAsync())
            {
                return false;
            }

            DateTime today = DateTime.Today;
            DateTime now = DateTime.UtcNow;

            var customer1 = new Customer
            {
                Name = "Demo Commercial Client",
                Email = "commercial@example.com",
                Phone = "321-555-0101",
                Address = "Demo Site 01 - Commercial Property"
            };

            var customer2 = new Customer
            {
                Name = "Demo Residential Client",
                Email = "residential@example.com",
                Phone = "321-555-0102",
                Address = "Demo Site 02 - Residential Property"
            };

            var project1 = new Project
            {
                Name = "Demo HVAC Retrofit",
                SiteAddress = customer1.Address,
                Description = "Mechanical system improvements.",
                Status = ProjectStatuses.Active,
                Customer = customer1
            };

            var project2 = new Project
            {
                Name = "Demo Office Renovation",
                SiteAddress = customer1.Address,
                Description = "Interior office improvements.",
                Status = ProjectStatuses.Active,
                Customer = customer1
            };

            var project3 = new Project
            {
                Name = "Demo Residential Renovation",
                SiteAddress = customer2.Address,
                Description = "Residential maintenance and repairs.",
                Status = ProjectStatuses.Completed,
                Customer = customer2
            };

            var technician1 = new Technician
            {
                Name = "Demo Technician A",
                Specialty = "HVAC",
                Phone = "321-555-0103"
            };

            var technician2 = new Technician
            {
                Name = "Demo Technician B",
                Specialty = "Electrical",
                Phone = "321-555-0104"
            };

            var technician3 = new Technician
            {
                Name = "Demo Technician C",
                Specialty = "Interior Finishes",
                Phone = "321-555-0105"
            };

            var issue1 = new SiteIssue
            {
                Title = "Uneven airflow",
                Description = "Airflow varies between adjacent rooms.",
                Category = "HVAC",
                SiteArea = "Second floor",
                Priority = "Normal",
                Status = SiteIssueStatuses.InProgress,
                CreatedAt = now.AddDays(-5),
                Customer = customer1,
                Project = project1
            };

            var issue2 = new SiteIssue
            {
                Title = "Damaged light fixture",
                Description = "Light fixture requires replacement.",
                Category = "Electrical",
                SiteArea = "Main hallway",
                Priority = "High",
                Status = SiteIssueStatuses.Open,
                CreatedAt = now.AddDays(-3),
                Customer = customer1,
                Project = project2
            };

            var issue3 = new SiteIssue
            {
                Title = "Damaged ceiling tile",
                Description = "Ceiling tile requires replacement.",
                Category = "Interior Finishes",
                SiteArea = "Living area",
                Priority = "Normal",
                Status = SiteIssueStatuses.Completed,
                CreatedAt = now.AddDays(-10),
                Customer = customer2,
                Project = project3
            };

            var issue4 = new SiteIssue
            {
                Title = "Loose cabinet hardware",
                Description = "Cabinet hardware requires adjustment.",
                Category = "Interior Finishes",
                SiteArea = "Break room",
                Priority = "Low",
                Status = SiteIssueStatuses.Open,
                CreatedAt = now.AddDays(-1),
                Customer = customer1,
                Project = project2
            };

            var workOrder1 = new WorkOrder
            {
                SiteIssue = issue1,
                Technician = technician1,
                ScheduledDate = today.AddDays(-1),
                Status = WorkOrderStatuses.InProgress,
                Notes = "Inspect and adjust airflow."
            };

            var workOrder2 = new WorkOrder
            {
                SiteIssue = issue2,
                Technician = technician2,
                ScheduledDate = today.AddDays(3),
                Status = WorkOrderStatuses.Pending,
                Notes = "Inspect and replace damaged fixture."
            };

            var workOrder3 = new WorkOrder
            {
                SiteIssue = issue3,
                Technician = technician3,
                ScheduledDate = today.AddDays(-7),
                Status = WorkOrderStatuses.Completed,
                Notes = "Replace damaged ceiling tile.",
                ResolutionNotes = "Ceiling tile replaced successfully.",
                CompletedAt = now.AddDays(-7)
            };

            context.Customers.AddRange(customer1, customer2);

            context.Projects.AddRange(
                project1, project2, project3);

            context.Technicians.AddRange(
                technician1, technician2, technician3);

            context.SiteIssues.AddRange(
                issue1, issue2, issue3, issue4);

            context.WorkOrders.AddRange(
                workOrder1, workOrder2, workOrder3);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
    }
}