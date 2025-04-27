using Microsoft.EntityFrameworkCore;
using ElstromVPKS.JWT;
using ElstromVPKS.Models;
using ElstromVPKS.Controllers;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Настройка JwtOptions
services.Configure<JwtOptions>(configuration.GetSection(nameof(JwtOptions)));

// Настройка аутентификации
services.AddApiAuthorization(configuration);

// Настройка сервисов
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

// Подключение базы данных
services.AddDbContext<ElstromContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

// Настройка CORS
services.AddCors(options =>
{
    options.AddPolicy("CORSPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:5173")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Настройка JwtProvider
services.AddSingleton<JwtProvider>();

var app = builder.Build();

// Настройка middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CORSPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }