using System;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Scalar.AspNetCore;
using System.Text.Json;
using SimpleGateway.DataAccess;

namespace SimpleGateway.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Build main gateway app (port 8000)
            var builderMain = WebApplication.CreateBuilder(args);
            builderMain.Configuration["AppMode"] = "Gateway";
            ConfigureServices(builderMain);
            builderMain.WebHost.ConfigureKestrel(opts => opts.ListenAnyIP(8000));
            var appMain = builderMain.Build();

            ConfigureGatewayPipeline(appMain);

            // Build admin app (port 8001)
            var builderAdmin = WebApplication.CreateBuilder(args);
            builderAdmin.Configuration["AppMode"] = "Admin";
            ConfigureServices(builderAdmin);
            builderAdmin.WebHost.ConfigureKestrel(opts => opts.ListenAnyIP(8001));
            var appAdmin = builderAdmin.Build();

            ConfigureAdminPipeline(appAdmin);

            using (var scope = appMain.Services.CreateScope())
            {
                Console.WriteLine("Applying database migrations...");
                var dbContext = scope.ServiceProvider.GetRequiredService<GatewayDbContext>();
                await dbContext.Database.MigrateAsync();
                Console.WriteLine("Database migrations applied successfully.");
            }

            // Run both apps
            await Task.WhenAll(appMain.RunAsync(), appAdmin.RunAsync());


        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.Configuration
                //.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                //.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var appMode = builder.Configuration["AppMode"];

            // Enable controllers and Razor views for admin UI
            builder.Services.AddControllersWithViews(options =>
            {
                // In Admin mode, exclude GatewayController to prevent routing conflicts
                if (appMode == "Admin")
                {
                    options.Conventions.Add(new ExcludeControllerConvention("Gateway"));
                }
            });

            builder.Services.AddOpenApi();

            builder.Services.AddHttpClient("singletonClient").SetHandlerLifetime(Timeout.InfiniteTimeSpan);
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("singletonClient"));
            builder.Services.AddSingleton<SimpleGateway.Api.Utils.HttpUtil>();

            // Prefer configuration (which includes environment variables) but fall back to Environment.GetEnvironmentVariable
            var uri = new Uri(builder.Configuration["POSTGRES_CONNECTION"] ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION"));

            var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]}";


            Console.WriteLine($"Using DB URI connection string: {uri}");
            Console.WriteLine($"Using DB connection string: {connectionString}");
            //Console.Write(JsonSerializer.Serialize(builder.Configuration.AsEnumerable().ToList()));
            //Console.WriteLine(JsonSerializer.Serialize(Environment.GetEnvironmentVariables()));
            // Log the fact that we resolved a connection string (mask password when printing)

            builder.Services.AddDbContext<GatewayDbContext>(options => options.UseNpgsql(connectionString));
        }

        private static void ConfigureGatewayPipeline(WebApplication app)
        {
            // Only map API controllers with attribute routing (will include GatewayController)
            // Since GatewayController has catch-all routes, it will handle all gateway traffic
            app.MapControllers();
        }

        private static void ConfigureAdminPipeline(WebApplication app)
        {
            app.MapOpenApi();
            app.MapScalarApiReference();

            // Serve static files and enable MVC routes for the admin UI
            app.UseStaticFiles();
            app.UseAuthorization();

            // Map MVC controllers (HomeController, ServicesController, EndpointsController) with conventional routing
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Map AdminController API routes (e.g., /admin/services)
            app.MapControllers();
        }
    }

    // Convention to exclude specific controllers from being discovered
    public class ExcludeControllerConvention : Microsoft.AspNetCore.Mvc.ApplicationModels.IApplicationModelConvention
    {
        private readonly string _controllerName;

        public ExcludeControllerConvention(string controllerName)
        {
            _controllerName = controllerName;
        }

        public void Apply(Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel application)
        {
            var controllerToRemove = application.Controllers
                .FirstOrDefault(c => c.ControllerName.Equals(_controllerName, StringComparison.OrdinalIgnoreCase));

            if (controllerToRemove != null)
            {
                application.Controllers.Remove(controllerToRemove);
            }
        }
    }
}
