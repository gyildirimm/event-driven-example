using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services;
using NotificationService.Infrastructure.BackgroundServices;
using NotificationService.Infrastructure.Services;
using Shared.Kernel.Application.Repositories;

namespace NotificationService.Infrastructure;

public static class DependencyInstaller
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ")!;
        services.AddKeyedSingleton<IEventPublisher>(
            "notification-events",
            (sp, key) =>
            {
                var l = sp.GetRequiredService<ILogger<RabbitMQEventPublisher>>();
                return new RabbitMQEventPublisher(l, rabbitMqConnectionString, "notification-events");
            });


        services.AddKeyedScoped<INotificationSenderService>("sms-sender", (provider, o) =>
        {
            var notificationService = provider.GetRequiredService<INotificationService>();
            var l = provider.GetRequiredService<ILogger<SmsSenderService>>();
            var uow = provider.GetRequiredService<IUnitOfWork>();
            
            return new SmsSenderService(notificationService, l, uow);
        });
        
        services.AddKeyedScoped<INotificationSenderService>("email-sender", (provider, o) =>
        {
            var notificationService = provider.GetRequiredService<INotificationService>();
            var l = provider.GetRequiredService<ILogger<EmailSenderService>>();
            var uow = provider.GetRequiredService<IUnitOfWork>();
            
            return new EmailSenderService(notificationService, l, uow);
        });

        
        services.AddSingleton<SmsConsumer>(provider =>
            new SmsConsumer(
                provider, 
                provider.GetRequiredService<ILogger<SmsConsumer>>(),
                rabbitMqConnectionString));

        services.AddHostedService(provider => provider.GetRequiredService<SmsConsumer>());
        
        services.AddSingleton<EmailConsumer>(provider =>
            new EmailConsumer(
                provider, 
                provider.GetRequiredService<ILogger<EmailConsumer>>(),
                rabbitMqConnectionString));
        
        services.AddHostedService(provider => provider.GetRequiredService<EmailConsumer>());
        
        return services;
    }
}