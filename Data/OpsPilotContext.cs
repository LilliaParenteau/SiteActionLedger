
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
    }
}
