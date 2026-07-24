using Microsoft.EntityFrameworkCore;
using PollService.Models;

namespace PollService.Data
{
    // Lớp quản lý kết nối và thao tác với Database PollDB thông qua EF Core
    public class PollDbContext : DbContext
    {
        // Constructor nhận cấu hình (Connection String, Provider...) truyền từ Program.cs
        public PollDbContext(DbContextOptions<PollDbContext> options) : base(options)
        {
        }

        // Khai báo bảng Polls tương ứng với tập dữ liệu các cuộc bình chọn
        public DbSet<Poll> Polls { get; set; }

        // Khai báo bảng Options tương ứng với tập dữ liệu các lựa chọn
        public DbSet<Option> Options { get; set; }
    }
}
