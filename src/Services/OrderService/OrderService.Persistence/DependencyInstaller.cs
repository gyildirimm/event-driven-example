using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Repositories;
using OrderService.Application.Services;
using OrderService.Persistence.BackgroundServices;
using OrderService.Persistence.Contexts;
using OrderService.Persistence.Repositories;
using OrderService.Persistence.Services;
using Shared.Kernel.Application.Repositories;
using Shared.Kernel.Persistence.Repositories;

namespace OrderService.Persistence;

public static class DependencyInstaller
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string orderContextConnectionString = configuration.GetConnectionString("OrderDatabase")!;
        
        services.AddDbContext<OrderContext>(options =>
        {
            options.UseNpgsql(orderContextConnectionString, o =>
            {
                o.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "order");
            });
        });
        
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderService, OrderApplicationService>();
        services.AddScoped<IUnitOfWork<OrderContext>, UnitOfWork<OrderContext>>();
        
        services.AddHostedService<OutboxService>();
        
        return services;
    }
}