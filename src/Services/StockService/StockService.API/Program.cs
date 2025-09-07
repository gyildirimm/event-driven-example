using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StockService.Application.Validations;
using StockService.Persistence;
using Shared.Kernel.Common.Middlewares;
using StockService.Persistence.Contexts;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// FluentValidation
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();

builder.Services.AddValidatorsFromAssemblyContaining<CreateStockRequestValidator>();

builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Stock API", 
        Version = "v1",
        Description = "Stock Service API"
    });
});

// Add Persistence
builder.Services.AddPersistence(builder.Configuration);

// Add Event Services
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!;
builder.Services.AddSingleton<StockService.Application.Services.IEventPublisher>(provider =>
    new StockService.Infrastructure.Services.RabbitMQEventPublisher(rabbitMqConnectionString, "stock-events"));

builder.Services.AddTransient<StockService.Infrastructure.EventHandlers.StockEventHandler>();

// Add Background Services
builder.Services.AddSingleton<StockService.Infrastructure.BackgroundServices.StockEventConsumer>(provider =>
    new StockService.Infrastructure.BackgroundServices.StockEventConsumer(
        provider, 
        provider.GetRequiredService<ILogger<StockService.Infrastructure.BackgroundServices.StockEventConsumer>>(),
        rabbitMqConnectionString));

builder.Services.AddHostedService(provider => provider.GetRequiredService<StockService.Infrastructure.BackgroundServices.StockEventConsumer>());

var app = builder.Build();

app.UsePathBase("/stock-api");

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // YARP routing ile /stock-api/swagger/v1/swagger.json doğrudan yönlendiriliyor
    c.SwaggerEndpoint("/stock-api/swagger/v1/swagger.json", "Stock API");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.MapControllers();

// Auto Migration - Uygulama başlatıldığında otomatik migration çalıştır
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<StockContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // PostgreSQL'in hazır olmasını bekle
        WaitForDatabase(dbContext, logger);
        
        // Migration'ları uygula - EnsureCreated ile Migrate birlikte kullanılmaz
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
        // Migration hatası durumunda uygulamayı durdurmak için exception'ı yeniden fırlat
        throw;
    }
}

static void WaitForDatabase(StockContext dbContext, ILogger<Program> logger, int maxRetries = 30, int delaySeconds = 2)
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
