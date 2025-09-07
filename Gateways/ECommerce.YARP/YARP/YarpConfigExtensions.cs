using System.Reflection;
using ECommerce.YARP.Flags;

namespace ECommerce.YARP.YARP;

public static class YarpConfigExtensions
{
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services)
    {
        var routeTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(IRouteProvider).IsAssignableFrom(t));

        foreach (var type in routeTypes)
            services.AddTransient(typeof(IRouteProvider), type);

        var clusterTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(IClusterProvider).IsAssignableFrom(t));

        foreach (var type in clusterTypes)
            services.AddTransient(typeof(IClusterProvider), type);
        
        return services;
    }

    public static WebApplicationBuilder AddGateway(this WebApplicationBuilder builder)
    {
        var routeProviders = builder.Services.BuildServiceProvider().GetServices<IRouteProvider>();
        var clusterProviders = builder.Services.BuildServiceProvider().GetServices<IClusterProvider>();

        var allRoutes = routeProviders.SelectMany(p => p.GetRoutes()).ToList();
        var allClusters = clusterProviders.SelectMany(p => p.GetClusters()).ToList();

        builder.Services
            .AddReverseProxy()
            .LoadFromMemory(allRoutes, allClusters);

        return builder;
    }
}