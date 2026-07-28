using Microsoft.EntityFrameworkCore;
using VoteService.Models;

namespace VoteService.Data
{
    // Lớp quản lý dữ liệu VoteDB bằng Entity Framework Core
    public class VoteDbContext : DbContext
    {
        public VoteDbContext(DbContextOptions<VoteDbContext> options) : base(options)
        {
        }

        // Bảng chứa dữ liệu danh sách lượt bình chọn
        public DbSet<Vote> Votes { get; set; }
    }
}
