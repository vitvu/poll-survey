using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình CORS cho phép Frontend truy cập không bị chặn
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Nạp file cấu hình ocelot.json
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Đăng ký Ocelot vào DI Container
builder.Services.AddOcelot();

var app = builder.Build();

app.UseCors("AllowAll");

// Sử dụng Ocelot Middleware để điều hướng Request
await app.UseOcelot();

app.Run();
