using Microsoft.EntityFrameworkCore;
using SiteActionLedger.Models;

namespace SiteActionLedger.Data
{
    public class SiteActionLedgerContext : DbContext
    {
        public SiteActionLedgerContext(
            DbContextOptions<SiteActionLedgerContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<SiteIssue> SiteIssues { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<Technician> Technicians { get; set; }
        public DbSet<Project> Projects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Technician>()
                .ToTable("Technician");

            modelBuilder.Entity<Project>()
                .ToTable("Projects");

            modelBuilder.Entity<SiteIssue>()
                .ToTable("ServiceRequests");

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Customer)
                .WithMany()
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SiteIssue>()
                .HasOne(i => i.Customer)
                .WithMany()
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SiteIssue>()
                .HasOne(i => i.Project)
                .WithMany()
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkOrder>()
                .HasOne(w => w.SiteIssue)
                .WithOne()
                .HasForeignKey<WorkOrder>(w => w.SiteIssueId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkOrder>()
                .HasOne(w => w.Technician)
                .WithMany()
                .HasForeignKey(w => w.TechnicianId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}