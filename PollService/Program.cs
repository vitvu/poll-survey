using Microsoft.EntityFrameworkCore;
using PollService.Data;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Đăng ký dịch vụ PollDbContext kết nối tới SQL Server dựa trên Connection String trong appsettings.json
builder.Services.AddDbContext<PollDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký bộ xử lý Controller API
builder.Services.AddControllers();

// Đăng ký dịch vụ Swagger để tạo giao diện kiểm thử API tự động
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");

// Cấu hình Middleware Swagger hiển thị trong môi trường Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Bỏ app.UseHttpsRedirection() để tránh lỗi 307 Temporary Redirect trên môi trường Local HTTP Microservices
app.UseAuthorization();
app.MapControllers();

app.Run();
