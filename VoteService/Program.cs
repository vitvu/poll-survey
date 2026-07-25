using Microsoft.EntityFrameworkCore;
using VoteService.Data;
using VoteService.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình CORS cho cả HTTP API và SignalR WebSocket
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000") // Frontend URLs
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR yêu cầu credentials
    });
});

// Đăng ký dịch vụ VoteDbContext với SQL Server (VoteDB)
builder.Services.AddDbContext<VoteDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký HttpClient factory để gọi các API liên dịch vụ (PollService & AnalyticsService)
builder.Services.AddHttpClient();

// Đăng ký SignalR cho real-time updates
builder.Services.AddSignalR();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Bỏ app.UseHttpsRedirection() tránh lỗi 307 Temporary Redirect trên Local Microservices
app.UseAuthorization();
app.MapControllers();

// Map SignalR Hub endpoint cho real-time vote updates
app.MapHub<VoteHub>("/hubs/vote");

app.Run();
