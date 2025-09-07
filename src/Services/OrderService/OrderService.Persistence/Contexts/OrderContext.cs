using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Entities;

namespace OrderService.Persistence.Contexts;

public class OrderContext : DbContext
{
    public OrderContext(DbContextOptions<OrderContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; } = default!;
    public DbSet<OrderLine> OrderLines { get; set; } = default!;
    public DbSet<OutboxEvent> OutboxEvents { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order Entity Configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CustomerId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.CustomerEmail)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired(false);

            // Ignore complex value objects for now - use simple mapping
            entity.Ignore(e => e.TotalAmount);

            // Add simple decimal property for total amount
            entity.Property<decimal>("TotalAmountValue")
                .HasPrecision(18, 2);
            
            // Relationships
            entity.HasMany(e => e.OrderLines)
                .WithOne(ol => ol.Order)
                .HasForeignKey(ol => ol.OrderId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // entity.HasMany(e => e.OutboxEvents)
            //     .WithOne()
            //     .HasForeignKey("OrderId")
            //     .IsRequired()
            //     .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderLine Entity Configuration
        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.OrderId)
                .IsRequired();
            
            entity.Property(e => e.ProductId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Quantity)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired(false);

            // Ignore complex value objects for now
            entity.Ignore(e => e.UnitPrice);
            entity.Ignore(e => e.TotalPrice);

            // Add simple decimal properties
            entity.Property<decimal>("UnitPriceValue")
                .HasPrecision(18, 2);

            entity.Property<decimal>("TotalPriceValue")
                .HasPrecision(18, 2);
        });

        // OutboxEvent Entity Configuration
        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // entity.Property<Guid>("OrderId").IsRequired(false); // Foreign key property eklendi

            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Data)
                .IsRequired();

            entity.Property(e => e.OccurredOn)
                .IsRequired();

            entity.Property(e => e.Processed)
                .IsRequired();

            entity.Property(e => e.ProcessedAt)
                .IsRequired(false);

            entity.Property(e => e.Error)
                .HasMaxLength(1000);

            entity.Property(e => e.RetryCount)
                .IsRequired();

            entity.Property(e => e.MaxRetries)
                .IsRequired();

            entity.HasIndex(e => new { e.Processed, e.OccurredOn })
                .HasDatabaseName("IX_OutboxEvents_Processed_OccurredOn");

            entity.HasIndex(e => e.Type)
                .HasDatabaseName("IX_OutboxEvents_Type");
        });
    }
}