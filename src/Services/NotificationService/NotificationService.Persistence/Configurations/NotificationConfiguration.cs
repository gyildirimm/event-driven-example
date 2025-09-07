using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        
        builder.HasKey(x => x.Id);

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(x => x.Channel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(x => x.Recipient)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(x => x.Subject)
            .HasMaxLength(500);
            
        builder.Property(x => x.Body)
            .HasColumnType("TEXT");
            
        builder.Property(x => x.Text)
            .HasMaxLength(1000);
            
        builder.Property(x => x.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(x => x.LastError)
            .HasColumnType("TEXT");
            
        builder.Property(x => x.SentAtUtc)
            .IsRequired(false);
            
        builder.Property(x => x.CreatedAt)
            .IsRequired();
            
        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);
        
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Channel);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.Status, x.Channel });
    }
}