using Estoque.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Api.Data
{
    public class EstoqueDbContext : DbContext
    {
        public EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProcessedRequest> ProcessedRequests => Set<ProcessedRequest>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(p => p.Code).IsUnique();
                entity.Property(p => p.Code).HasMaxLength(50).IsRequired();
                entity.Property(p => p.Description).HasMaxLength(500).IsRequired();
            });

            modelBuilder.Entity<ProcessedRequest>(entity =>
            {
                entity.HasIndex(r => r.IdempotencyKey).IsUnique();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
