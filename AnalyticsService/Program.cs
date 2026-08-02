using AnalyticsService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// configure cors policy to allow frontend requests
builder.Services.AddCors(options =>
{
    // add a policy named allowall
    options.AddPolicy("AllowAll", policy =>
    {
        // allow requests from frontend urls
        policy.WithOrigins("http://localhost:8080", "https://localhost:5173")
              // accept any headers in request
              .AllowAnyHeader()
              // accept any http methods (get, post, etc)
              .AllowAnyMethod();
    });
});

// register analyticsdbcontext with sql server database
builder.Services.AddDbContext<AnalyticsDbContext>(options =>
    // configure to use sql server with connection string from appsettings
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// register controllers service
builder.Services.AddControllers();
// register endpoint explorer for swagger
builder.Services.AddEndpointsApiExplorer();
// register swagger generator
builder.Services.AddSwaggerGen();

// build the app
var app = builder.Build();

// use cors middleware with allowall policy
app.UseCors("AllowAll");

// check if running in development environment
if (app.Environment.IsDevelopment())
{
    // enable swagger ui for api documentation
    app.UseSwagger();
    // enable swagger ui interface
    app.UseSwaggerUI();
}

// enable https redirect middleware
app.UseHttpsRedirection();
// enable authorization middleware
app.UseAuthorization();
// map controller endpoints
app.MapControllers();

// start the application
app.Run();
