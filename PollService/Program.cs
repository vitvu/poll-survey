using Microsoft.EntityFrameworkCore;
using PollService.Data;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Đăng ký dịch vụ PollDbContext kết nối tới SQL Server dựa trên Connection String trong appsettings.json
builder.Services.AddDbContext<PollDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký IHttpClientFactory (dùng để gọi VoteService khi đóng poll)
builder.Services.AddHttpClient();

// Đăng ký bộ xử lý Controller API
// Cấu hình JSON: serialize DateTime luôn theo format "R" (RFC1123) hoặc dùng
// DateTimeZoneHandling để thêm "Z" khi Kind = Utc
// Cách đơn giản nhất: thay đổi serializer mặc định sang Newtonsoft.Json
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        // DateTimeZoneHandling.Utc: tất cả DateTime khi serialize đều thêm "Z"
        // → JS nhận "2026-08-02T06:42:00Z" → parse đúng là UTC → hiển thị đúng giờ local
        options.SerializerSettings.DateTimeZoneHandling =
            Newtonsoft.Json.DateTimeZoneHandling.Utc;
    });

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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
