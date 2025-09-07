using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockService.Domain.Entities;

namespace StockService.Persistence.Configurations;

public class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ProductId)
            .IsRequired()
            .HasComment("The product identifier for this stock entry");

        builder.Property(e => e.Quantity)
            .IsRequired()
            .HasComment("Total quantity of the product in stock");

        builder.Property(e => e.ReservedQuantity)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Quantity reserved for pending orders");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasComment("When the stock entry was created");

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false)
            .HasComment("When the stock entry was last updated");

        // Computed property (read-only) - ignored in database mapping
        builder.Ignore(e => e.AvailableQuantity);

        // Indexes for better performance
        builder.HasIndex(e => e.ProductId)
            .IsUnique()
            .HasDatabaseName("IX_Stocks_ProductId");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Stocks_CreatedAt");

        builder.HasIndex(e => new { e.ProductId, e.Quantity })
            .HasDatabaseName("IX_Stocks_ProductId_Quantity");

        // Table configuration
        builder.ToTable("Stocks", schema: "stock");
    }
}
