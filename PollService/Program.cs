using Microsoft.EntityFrameworkCore;
using PollService.Data;

var builder = WebApplication.CreateBuilder(args);

// đọc danh sách origin được phép từ config (hỗ trợ cả local và cloud)
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:8080", "http://localhost:5173" };

// configure cors policy to allow frontend requests
builder.Services.AddCors(options =>
{
    // add a policy named allowall
    options.AddPolicy("AllowAll", policy =>
    {
        // allow requests from configured origins
        policy.WithOrigins(allowedOrigins)
              // accept any headers in request
              .AllowAnyHeader()
              // accept any http methods (get, post, etc)
              .AllowAnyMethod();
    });
});

// register polldbcontext with mysql database
builder.Services.AddDbContext<PollDbContext>(options =>
    // configure to use mysql with connection string from config/env
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// register http client factory for inter-service calls
builder.Services.AddHttpClient();

// register controllers service
builder.Services.AddControllers()
    // configure json serialization for controller responses
    .AddNewtonsoftJson(options =>
    {
        // set datetime zone handling to always include z for utc times
        options.SerializerSettings.DateTimeZoneHandling =
            Newtonsoft.Json.DateTimeZoneHandling.Utc;
    });

// register endpoint explorer for swagger
builder.Services.AddEndpointsApiExplorer();
// register swagger generator
builder.Services.AddSwaggerGen();

// build the app
var app = builder.Build();

// auto-create tables if they don't exist (runs on startup)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PollDbContext>();
    db.Database.EnsureCreated();
}

// use cors middleware with allowall policy
app.UseCors("AllowAll");

// enable swagger ui for all environments (also used as health check endpoint)
app.UseSwagger();
app.UseSwaggerUI();

// enable authorization middleware
app.UseAuthorization();
// map controller endpoints
app.MapControllers();

// start the application
app.Run();
