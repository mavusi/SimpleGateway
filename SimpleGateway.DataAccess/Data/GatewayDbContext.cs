using Microsoft.EntityFrameworkCore;
using SimpleGateway.DataAccess.Models;

namespace SimpleGateway.DataAccess
{
    public class GatewayDbContext : DbContext
    {
        public GatewayDbContext(DbContextOptions<GatewayDbContext> options) : base(options)
        {
        }

        public DbSet<GatewayService> Services { get; set; }
        public DbSet<GatewayEndpoint> Endpoints { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GatewayService>(b =>
            {
                b.HasKey(s => s.Id);
                b.Property(s => s.Id).IsRequired();
                b.Property(s => s.Name);
                b.Property(s => s.Url);
                b.Property(s => s.Path);
                b.HasMany(s => s.Endpoints).WithOne().HasForeignKey(e => e.ServiceId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<GatewayEndpoint>(b =>
            {
                b.HasKey(e => e.Id);
                b.Property(e => e.Id).IsRequired();
                b.Property(e => e.ServiceId).IsRequired();
                b.Property(e => e.Method);
                b.Property(e => e.Path);
            });
        }
    }
}
