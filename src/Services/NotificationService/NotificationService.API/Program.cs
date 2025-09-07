using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NotificationService.Application.Validators;
using NotificationService.Infrastructure;
using NotificationService.Persistence;
using NotificationService.Persistence.Contexts;
using Shared.Kernel.Common.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// FluentValidation yapılandırması
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<NotificationSmsCreateModelValidator>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Notification API", 
        Version = "v1",
        Description = "Notification Service API"
    });
});

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UsePathBase("/notification-api");

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/notification-api/swagger/v1/swagger.json", "notification API");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
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
}

static void WaitForDatabase(NotificationContext dbContext, ILogger<Program> logger, int maxRetries = 30, int delaySeconds = 2)
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

app.Run();