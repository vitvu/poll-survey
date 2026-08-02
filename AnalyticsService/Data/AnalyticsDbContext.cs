using AnalyticsService.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Data
{
    public class AnalyticsDbContext : DbContext
    {
        // constructor receives database options from dependency injection
        public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> databaseOptions) 
            // pass options to parent dbcontext class
            : base(databaseOptions)
        {
        }

        // dbset property maps to the analytics table in database
        public DbSet<Analytics> Analytics { get; set; }
    }
}
