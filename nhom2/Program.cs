using System;
using Microsoft.EntityFrameworkCore;
using nhom2.Infrastructure.Data;
using nhom2.Application.Services;
using nhom2.Domain.Interfaces;
using nhom2.Infrastructure.Repositories;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000", "https://0.0.0.0:5001");

// Đăng ký DbContext với SQLite
var connectionString = "Data Source=nhom2.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString)
);

// Đăng ký Dependency Injection
// Repository
builder.Services.AddScoped<IOrder, OrderRepo>();

// Service
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddHttpClient<IUserClient, UserClient>(client =>
{
    var baseUrl = builder.Configuration["Services:User:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Services:User:BaseUrl is not configured.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Thêm Controllers - chỉ định rõ Assembly
builder.Services.AddControllers()
    .AddApplicationPart(typeof(nhom2.Api.Order.OrderController).Assembly);

// Thêm Swagger/OpenAPI (tùy chọn, để test API dễ hơn)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhap 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Thêm CORS (frontend có thể gọi API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
       policy.WithOrigins("http://192.168.31.118:5173", "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// BUILD APP
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Áp dụng migration 
    dbContext.Database.Migrate();
  
    Console.WriteLine("✅ Database initialized successfully!");
}

// Swagger UI - LUÔN BẬT để test API dễ hơn
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS redirect
// app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowFrontend");

// Authentication/Authorization 
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// run app
Console.WriteLine("Server starting on https://0.0.0.0:5001 and http://0.0.0.0:5000");
app.Run();
