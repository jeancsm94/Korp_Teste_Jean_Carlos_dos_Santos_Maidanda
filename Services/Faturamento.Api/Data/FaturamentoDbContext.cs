using Faturamento.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.Api.Data
{
    public class FaturamentoDbContext : DbContext
    {
        public FaturamentoDbContext(DbContextOptions<FaturamentoDbContext> options) : base(options)
        {
        }

        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<InvoiceCounter> InvoiceCounters => Set<InvoiceCounter>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(i => i.Number).IsUnique();
                entity.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
                entity.HasMany(i => i.Items)
                    .WithOne(item => item.Invoice!)
                    .HasForeignKey(item => item.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<InvoiceCounter>().HasData(new InvoiceCounter { Id = 1, NextNumber = 1 });

            base.OnModelCreating(modelBuilder);
        }
    }
}
