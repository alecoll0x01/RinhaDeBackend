
using Microsoft.EntityFrameworkCore;
using RinhaDeBackend.Data;
using RinhaDeBackend.Service;
using StackExchange.Redis;
using System.Text.Json;

namespace RinhaDeBackend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseUrls("http://+:8080");
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });

        // Database
        builder.Services.AddDbContext<PaymentContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Redis
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = sp.GetService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Redis") ?? "redis:6379";
            return ConnectionMultiplexer.Connect(connectionString);
        });

        // HTTP Clients para Payment Processors
        builder.Services.AddHttpClient("default", client =>
        {
            var defaultUrl = builder.Configuration["PaymentProcessors:Default"] ?? "http://payment-processor-default:8080";
            client.BaseAddress = new Uri(defaultUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddHttpClient("fallback", client =>
        {
            var fallbackUrl = builder.Configuration["PaymentProcessors:Fallback"] ?? "http://payment-processor-fallback:8080";
            client.BaseAddress = new Uri(fallbackUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Services
        builder.Services.AddScoped<IPaymentProcessorService, PaymentProcessorService>();
        builder.Services.AddScoped<IPaymentService, PaymentService>();
        builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

        // Health checks
        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres")
            .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "redis:6379", name: "redis");

        // Configurar Kestrel para performance
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        app.UseRouting();

        app.MapControllers();
        app.MapHealthChecks("/health");

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PaymentContext>();
            try
            {
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration info: {ex.Message}");
            }
        }

        app.Run();
    }
}
