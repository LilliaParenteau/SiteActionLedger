using Microsoft.EntityFrameworkCore;
using  SiteActionLedger.Models;

namespace  SiteActionLedger.Data
{
    public class  SiteActionLedgerContext : DbContext
    {
        public  SiteActionLedgerContext(DbContextOptions< SiteActionLedgerContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }
        public DbSet<Technician> Technicians { get; set; }
        public DbSet<Project> Projects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Technician>()
                .ToTable("Technician");

            modelBuilder.Entity<ServiceRequest>()
                .HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkOrder>()
                .HasOne(w => w.ServiceRequest)
                .WithOne()
                .HasForeignKey<WorkOrder>(w => w.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkOrder>()
                .HasOne(w => w.Technician)
                .WithMany()
                .HasForeignKey(w => w.TechnicianId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Project>()
                .ToTable("Projects");

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Customer)
                .WithMany()
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
