using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Kernel.Common.Extensions;

public static class DatabaseExtensions
{
    public static WebApplication UseDatabaseMigrate<TContext, TProgram>(this WebApplication app) 
        where TContext : DbContext
        where TProgram : class
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TProgram>>();
    
        try
        {
            WaitForDatabase(dbContext, logger);
        
            if (dbContext.Database.GetPendingMigrations().Any())
            {
                logger.LogInformation("Applying pending migrations...");
                dbContext.Database.Migrate();
                logger.LogInformation("Database migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("No pending migrations found.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database: {Message}", ex.Message);
            throw;
        }

        return app;
    }
    
    static void WaitForDatabase(DbContext dbContext, ILogger logger, int maxRetries = 30, int delaySeconds = 2)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                dbContext.Database.CanConnect();
                logger.LogInformation("Successfully connected to the database.");
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Database connection attempt {Attempt}/{MaxRetries} failed: {Message}", 
                    i + 1, maxRetries, ex.Message);
            
                if (i == maxRetries - 1)
                {
                    logger.LogError("Failed to connect to database after {MaxRetries} attempts.", maxRetries);
                    throw;
                }
            
                Task.Delay(TimeSpan.FromSeconds(delaySeconds)).Wait();
            }
        }
    }
}