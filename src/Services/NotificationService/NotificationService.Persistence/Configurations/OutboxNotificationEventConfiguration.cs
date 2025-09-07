using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Persistence.Configurations;

public class OutboxNotificationEventConfiguration : IEntityTypeConfiguration<OutboxNotificationEvent>
{
    public void Configure(EntityTypeBuilder<OutboxNotificationEvent> builder)
    {
        builder.ToTable("OutboxNotificationEvents");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(x => x.Data)
            .IsRequired()
            .HasColumnType("TEXT");
            
        builder.Property(x => x.ExchangeName)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(x => x.OccurredOn)
            .IsRequired();
            
        builder.Property(x => x.Processed)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(x => x.ProcessedAt)
            .IsRequired(false);
            
        builder.Property(x => x.Error)
            .HasColumnType("TEXT");
            
        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(x => x.MaxRetries)
            .IsRequired()
            .HasDefaultValue(3);
            
        builder.Property(x => x.NextTryAtUtc)
            .IsRequired(false);
            
        builder.Property(x => x.CreatedAt)
            .IsRequired();
            
        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);
            
        builder.HasIndex(x => x.Processed);
        builder.HasIndex(x => x.OccurredOn);
        builder.HasIndex(x => x.NextTryAtUtc);
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => x.ExchangeName);
        builder.HasIndex(x => new { x.Processed, x.NextTryAtUtc });
        builder.HasIndex(x => new { x.Processed, x.RetryCount });
    }
}