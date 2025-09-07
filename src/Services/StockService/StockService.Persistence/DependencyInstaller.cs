using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.Application.Repositories;
using Shared.Kernel.Persistence.Repositories;
using StockService.Application.Repositories;
using StockService.Application.Services;
using StockService.Persistence.Contexts;
using StockService.Persistence.Repositories;
using StockService.Persistence.Services;

namespace StockService.Persistence;

public static class DependencyInstaller
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string orderContextConnectionString = configuration.GetConnectionString("StockDatabase")!;
        
        services.AddDbContext<StockContext>(options =>
        {
            options.UseNpgsql(orderContextConnectionString, o =>
            {
                o.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "stock");
            });
        });
        
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IStockService, StockApplicationService>();
        services.AddScoped<IUnitOfWork<StockContext>, UnitOfWork<StockContext>>();
        
        return services;
    }
}