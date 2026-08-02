using Microsoft.EntityFrameworkCore;
using VoteService.Data;
using VoteService.Hubs;

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
              .AllowAnyMethod()
              // allow credentials for signalr websocket
              .AllowCredentials();
    });
});

// register votedbcontext with sql server database
builder.Services.AddDbContext<VoteDbContext>(options =>
    // configure to use sql server with connection string from appsettings
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// map signalr hub for real-time vote updates
app.MapHub<VoteHub>("/hubs/vote");

// start the application
app.Run();
