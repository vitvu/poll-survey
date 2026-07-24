using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Nạp file cấu hình ocelot.json
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Đăng ký Ocelot vào DI Container
builder.Services.AddOcelot();

var app = builder.Build();

// Sử dụng Ocelot Middleware để điều hướng Request
await app.UseOcelot();

app.Run();
