using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Persistence.Configurations;

namespace NotificationService.Persistence.Contexts;

public class NotificationContext(DbContextOptions<NotificationContext> options) : DbContext(options)
{
    public DbSet<Notification> Notification { get; set; }
    public DbSet<OutboxNotificationEvent> OutboxNotificationEvents { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationConfiguration).Assembly);
    }
}