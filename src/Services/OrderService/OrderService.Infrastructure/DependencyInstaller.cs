using Microsoft.Extensions.DependencyInjection;
using OrderService.Infrastructure.BackgroundServices;
using OrderService.Infrastructure.EventHandlers;

namespace OrderService.Infrastructure;

public static class DependencyInstaller
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services;
    }
}
