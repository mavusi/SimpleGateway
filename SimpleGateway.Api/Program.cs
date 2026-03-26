
using System;
using Microsoft.EntityFrameworkCore;
using SimpleGateway.Api.Data;
using System.Threading.Tasks;
using Scalar.AspNetCore;

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
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            
            builder.Services.AddHttpClient("singletonClient").SetHandlerLifetime(Timeout.InfiniteTimeSpan);
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("singletonClient"));
            builder.Services.AddSingleton<SimpleGateway.Api.Utils.HttpUtil>();

            var envConn = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
            var connectionString = !string.IsNullOrWhiteSpace(envConn)
                ? envConn
                : "Host=localhost;Database=gatewaydb;Username=postgres;Password=postgres";

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
