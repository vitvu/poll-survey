using Microsoft.EntityFrameworkCore;
using PollService.Models;

namespace PollService.Data
{
    public class PollDbContext : DbContext
    {
        // constructor receives database options from dependency injection
        public PollDbContext(DbContextOptions<PollDbContext> databaseOptions) 
            // pass options to parent dbcontext class
            : base(databaseOptions)
        {
        }

        // dbset property maps to the polls table in database
        public DbSet<Poll> Polls { get; set; }

        // dbset property maps to the options table in database
        public DbSet<Option> Options { get; set; }
    }
}
