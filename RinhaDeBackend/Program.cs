
using Microsoft.EntityFrameworkCore;
using RinhaDeBackend.Data;
using RinhaDeBackend.Services;
using System.Text.Json;

namespace RinhaDeBackend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //builder.Services.AddDbContext<PaymentContext>(options =>
        //    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddHttpClient<IPaymentProcessorService, PaymentProcessorService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddScoped<IPaymentProcessorService, PaymentProcessorService>();
        builder.Services.AddScoped<IPaymentService, PaymentService>();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

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

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PaymentContext>();
            context.Database.Migrate();
        }

        app.Run();
    }
}
