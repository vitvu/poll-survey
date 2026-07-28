using AnalyticsService.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Data
{
    // Lớp DbContext quản lý kết nối cơ sở dữ liệu AnalyticsDB
    public class AnalyticsDbContext : DbContext
    {
        public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
        {
        }

        // Tập dữ liệu phục vụ thống kê phân tích
        public DbSet<Analytics> Analytics { get; set; }
    }
}
