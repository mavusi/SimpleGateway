using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SimpleGateway.DataAccess
{
    public class GatewayDbContextFactory : IDesignTimeDbContextFactory<GatewayDbContext>
    {
        public GatewayDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<GatewayDbContext>();
            
            // Use a connection string for design-time (migrations)
            // This will be overridden at runtime by the actual configuration
            var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback connection string for local development
                connectionString = "Host=localhost;Port=5432;Database=simplegateway;Username=postgres;Password=postgres";
            }
            else if (connectionString.StartsWith("postgres://"))
            {
                // Parse postgres:// URI format
                var uri = new Uri(connectionString);
                connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]}";
            }
            
            optionsBuilder.UseNpgsql(connectionString);
            
            return new GatewayDbContext(optionsBuilder.Options);
        }
    }
}
