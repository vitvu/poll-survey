using Microsoft.EntityFrameworkCore;
using VoteService.Data;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ VoteDbContext với SQL Server (VoteDB)
builder.Services.AddDbContext<VoteDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký HttpClient factory để gọi các API liên dịch vụ (PollService & AnalyticsService)
builder.Services.AddHttpClient();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
