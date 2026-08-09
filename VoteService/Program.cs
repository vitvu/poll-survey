using Microsoft.EntityFrameworkCore;
using VoteService.Data;
using VoteService.Hubs;

var builder = WebApplication.CreateBuilder(args);

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
              .AllowAnyMethod()
              // allow credentials for signalr websocket
              .AllowCredentials();
    });
});

// register votedbcontext with mysql database
builder.Services.AddDbContext<VoteDbContext>(options =>
    // configure to use mysql with connection string from config/env
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// register http client factory for inter-service calls
builder.Services.AddHttpClient();

// register signalr for real-time vote updates
builder.Services.AddSignalR();

// register controllers service
builder.Services.AddControllers();
// register endpoint explorer for swagger
builder.Services.AddEndpointsApiExplorer();
// register swagger generator
builder.Services.AddSwaggerGen();

// build the app
var app = builder.Build();

// auto-create tables if they don't exist (runs on startup)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<VoteDbContext>();
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to ensure database created");
        throw;
    }
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

// map signalr hub for real-time vote updates
app.MapHub<VoteHub>("/hubs/vote");

// start the application
app.Run();
