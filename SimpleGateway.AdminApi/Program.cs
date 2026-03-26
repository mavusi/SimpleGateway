using Microsoft.EntityFrameworkCore;
using SimpleGateway.DataAccess;

namespace SimpleGateway.AdminApi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // Configure database
            var postgresConnection = builder.Configuration["POSTGRES_CONNECTION"] 
                ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
                ?? throw new InvalidOperationException("POSTGRES_CONNECTION environment variable is required");

            var uri = new Uri(postgresConnection);
            var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]}";

            builder.Services.AddDbContext<GatewayDbContext>(options => 
                options.UseNpgsql(connectionString));

            var app = builder.Build();

            // Apply database migrations on startup
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                try
                {
                    logger.LogInformation("Applying database migrations...");
                    var dbContext = scope.ServiceProvider.GetRequiredService<GatewayDbContext>();
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while applying database migrations.");
                    throw;
                }
            }

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
