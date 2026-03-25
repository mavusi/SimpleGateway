
using System;
using Microsoft.EntityFrameworkCore;
using SimpleGateway.Api.Data;

namespace SimpleGateway.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // Configure IHttpClientFactory and register a singleton HttpClient
            builder.Services.AddHttpClient("singletonClient")
                .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

            builder.Services.AddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("singletonClient"));

            // Register HttpUtil in DI
            builder.Services.AddSingleton<SimpleGateway.Api.Utils.HttpUtil>();

            // Configure PostgreSQL DbContext. Connection string can be provided via GATEWAYDB_CONNECTION env var.
            var envConn = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
            var connectionString = !string.IsNullOrWhiteSpace(envConn)
                ? envConn
                : "Host=localhost;Database=gatewaydb;Username=postgres;Password=postgres";

            builder.Services.AddDbContext<GatewayDbContext>(options =>
                options.UseNpgsql(connectionString)
            );

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
