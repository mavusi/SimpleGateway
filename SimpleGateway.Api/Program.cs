
using System;
using Microsoft.EntityFrameworkCore;
using SimpleGateway.Api.Data;
using System.Threading.Tasks;
using Scalar.AspNetCore;
using System.Text.Json;

namespace SimpleGateway.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Build main gateway app (port 8000)
            var builderMain = WebApplication.CreateBuilder(args);
            ConfigureServices(builderMain);
            builderMain.WebHost.ConfigureKestrel(opts => opts.ListenAnyIP(8000));
            var appMain = builderMain.Build();
            
            ConfigurePipeline(appMain);

            // Build admin app (port 8001)
            var builderAdmin = WebApplication.CreateBuilder(args);
            ConfigureServices(builderAdmin);
            builderAdmin.WebHost.ConfigureKestrel(opts => opts.ListenAnyIP(8001));
            var appAdmin = builderAdmin.Build();
            
            ConfigurePipeline(appAdmin);

            // Run both apps
            await Task.WhenAll(appMain.RunAsync(), appAdmin.RunAsync());
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.Configuration
                //.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                //.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            
            builder.Services.AddHttpClient("singletonClient").SetHandlerLifetime(Timeout.InfiniteTimeSpan);
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("singletonClient"));
            builder.Services.AddSingleton<SimpleGateway.Api.Utils.HttpUtil>();

            // Prefer configuration (which includes environment variables) but fall back to Environment.GetEnvironmentVariable
            var envConn = builder.Configuration["POSTGRES_CONNECTION"] ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
            var connectionString = !string.IsNullOrWhiteSpace(envConn)
                ? envConn
                : "Host=wronghost;Database=gatewaydb;Username=postgres;Password=postgres";

            //Console.Write(JsonSerializer.Serialize(builder.Configuration.AsEnumerable().ToList()));
            Console.WriteLine(JsonSerializer.Serialize(Environment.GetEnvironmentVariables()));
            // Log the fact that we resolved a connection string (mask password when printing)
            try
            {
                var masked = connectionString;
                var pwdIndex = masked?.IndexOf("Password=", StringComparison.OrdinalIgnoreCase) ?? -1;
                if (pwdIndex >= 0)
                {
                    var semicolon = masked.IndexOf(';', pwdIndex);
                    if (semicolon > pwdIndex)
                        masked = masked.Substring(0, pwdIndex + 9) + "***" + masked.Substring(semicolon);
                    else
                        masked = masked.Substring(0, pwdIndex + 9) + "***";
                }
                Console.WriteLine($"Using DB connection string: {masked}");
            }
            catch
            {
                // Ignore any logging failure during startup
            }

            builder.Services.AddDbContext<GatewayDbContext>(options => options.UseNpgsql(connectionString));
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            app.MapScalarApiReference();

            app.UseAuthorization();
            app.MapControllers();
        }
    }
}
