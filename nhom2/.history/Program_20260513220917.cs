using Microsoft.EntityFrameworkCore;
using nhom2.Infrastructure.Data;
using nhom2.Application.Services;
using nhom2.Domain.Interfaces;
using nhom2.Infrastructure.Repositories;

var builder = WebApplicationBuilder.CreateBuilder(args);

// ===== CONFIGURATION =====

// 1. Đăng ký DbContext với SQLite
var connectionString = "Data Source=nhom2.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString)
);

// 2. Đăng ký Dependency Injection
// Repository
builder.Services.AddScoped<IOrder, OrderRepo>();

// Service
builder.Services.AddScoped<IOrderService, OrderService>();

// 3. Thêm Controllers
builder.Services.AddControllers();

// 4. Thêm Swagger/OpenAPI (tùy chọn, để test API dễ hơn)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 5. Thêm CORS (để frontend có thể gọi API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// ===== BUILD APP =====
var app = builder.Build();

// ===== MIDDLEWARE CONFIGURATION =====

// 1. Tự động tạo database khi chạy ứng dụng
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Áp dụng migration (tạo table nếu chưa có)
    dbContext.Database.Migrate();
    
    // Nếu không có migration, có thể dùng:
    // dbContext.Database.EnsureCreated();
    
    Console.WriteLine("✅ Database initialized successfully!");
}

// 2. Swagger UI (chỉ khi development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 3. HTTPS redirect
app.UseHttpsRedirection();

// 4. Enable CORS
app.UseCors("AllowAll");

// 5. Authentication/Authorization (nếu có)
app.UseAuthentication();
app.UseAuthorization();

// 6. Map Controllers
app.MapControllers();

// ===== RUN APP =====
Console.WriteLine("🚀 Server starting on https://localhost:5001 and http://localhost:5000");
app.Run();
