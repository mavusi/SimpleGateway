using Microsoft.EntityFrameworkCore;
using SimpleGateway.Api.Models;

namespace SimpleGateway.Api.Data
{
    public class GatewayDbContext : DbContext
    {
        public GatewayDbContext(DbContextOptions<GatewayDbContext> options) : base(options)
        {
        }

        public DbSet<ServiceConfig> Services { get; set; } = null!;
        public DbSet<EndpointConfig> Endpoints { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServiceConfig>().HasKey(s => s.Id);
            modelBuilder.Entity<EndpointConfig>().HasKey(e => e.Id);
            base.OnModelCreating(modelBuilder);
        }
    }
}
