
using Microsoft.EntityFrameworkCore;
using OpsPilot.Models;

namespace OpsPilot.Data
{
    public class OpsPilotContext : DbContext
    {
        public OpsPilotContext(DbContextOptions<OpsPilotContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<ServiceRequest> ServiceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServiceRequest>()
                .HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
