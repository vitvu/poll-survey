using Microsoft.EntityFrameworkCore;
using VoteService.Data;
using VoteService.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p =>
    {
        p.WithOrigins(builder.Configuration
                .GetSection("AllowedOrigins")
                .Get<string[]>() ?? new[] { "http://localhost:8080", "http://localhost:5173" })
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials();
    });
});

builder.Services.AddDbContext<VoteDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 0))));

builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();
app.MapHub<VoteHub>("/hubs/vote");

app.Run();
