using Microsoft.EntityFrameworkCore;
using StockService.Domain.Entities;
using StockService.Persistence.Configurations;

namespace StockService.Persistence.Contexts;

public class StockContext(DbContextOptions<StockContext> options) : DbContext(options)
{
    public DbSet<Stock> Stocks { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockConfiguration).Assembly);
    }
}
