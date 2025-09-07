using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Repositories;
using NotificationService.Application.Services;
using NotificationService.Persistence.BackgroundServices;
using NotificationService.Persistence.Contexts;
using NotificationService.Persistence.Repositories;
using Shared.Kernel.Application.Repositories;
using Shared.Kernel.Persistence.Repositories;

namespace NotificationService.Persistence;

public static class DependencyInstaller
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string orderContextConnectionString = configuration.GetConnectionString("NotificationDatabase")!;
        
        services.AddDbContext<NotificationContext>(options =>
        {
            options.UseNpgsql(orderContextConnectionString, o =>
            {
                o.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "notification");
            });
        });
        
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IOutboxNotificationEventRepository, OutboxNotificationEventRepository>();
        services.AddScoped<INotificationService, Services.NotificationService>();
        services.AddScoped<IUnitOfWork<NotificationContext>, UnitOfWork<NotificationContext>>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<IUnitOfWork<NotificationContext>>());

        services.AddHostedService<OutboxNotificationService>();
        return services;
    }
}