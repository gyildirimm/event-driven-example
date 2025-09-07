using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrderService.Application.Services;
using OrderService.Application.Validations;
using OrderService.Infrastructure;
using OrderService.Infrastructure.BackgroundServices;
using OrderService.Infrastructure.EventHandlers;
using OrderService.Infrastructure.Services;
using OrderService.Persistence;
using OrderService.Persistence.Contexts;
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


builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();

builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Order API", 
        Version = "v1",
        Description = "Order Service API"
    });
});

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure();

var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!;
builder.Services.AddKeyedSingleton<IEventPublisher>(
    "stock-events",
    (sp, key) =>
    {
        var l = sp.GetRequiredService<ILogger<RabbitMQEventPublisher>>();
        return new RabbitMQEventPublisher(l, rabbitMqConnectionString, "stock-events");
    });

builder.Services.AddKeyedSingleton<IEventPublisher>(
    "order-events",
    (sp, key) =>
    {
        var l = sp.GetRequiredService<ILogger<RabbitMQEventPublisher>>();
        return new RabbitMQEventPublisher(l, rabbitMqConnectionString, "order-events");
    });

builder.Services.AddTransient<StockEventHandler>();

builder.Services.AddSingleton<StockEventListener>(provider =>
    new StockEventListener(
        provider, 
        provider.GetRequiredService<ILogger<StockEventListener>>(),
        rabbitMqConnectionString));

builder.Services.AddHostedService(provider => provider.GetRequiredService<StockEventListener>());

var app = builder.Build();

app.UsePathBase("/order-api");

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // YARP routing ile /order-api/swagger/v1/swagger.json doğrudan yönlendiriliyor
    c.SwaggerEndpoint("/order-api/swagger/v1/swagger.json", "Order API");
    c.RoutePrefix = "swagger"; // /order-api/swagger/ altında çalışacak
});

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderContext>();
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

static void WaitForDatabase(OrderContext dbContext, ILogger<Program> logger, int maxRetries = 30, int delaySeconds = 2)
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
