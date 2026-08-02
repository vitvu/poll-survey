using Microsoft.EntityFrameworkCore;
using VoteService.Models;

namespace VoteService.Data
{
    public class VoteDbContext : DbContext
    {
        // constructor receives database options from dependency injection
        public VoteDbContext(DbContextOptions<VoteDbContext> databaseOptions) 
            // pass options to parent dbcontext class
            : base(databaseOptions)
        {
        }

        // dbset property maps to the votes table in database
        public DbSet<Vote> Votes { get; set; }
    }
}
