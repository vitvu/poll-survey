using Microsoft.EntityFrameworkCore;
using VoteService.Models;

namespace VoteService.Data
{
    public class VoteDbContext : DbContext
    {
        public VoteDbContext(DbContextOptions<VoteDbContext> options) : base(options) { }

        public DbSet<Vote> Votes { get; set; }
    }
}
