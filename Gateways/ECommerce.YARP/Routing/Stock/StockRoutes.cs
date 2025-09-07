using ECommerce.YARP.Flags;
using Yarp.ReverseProxy.Configuration;

namespace ECommerce.YARP.Routing.Stock;

public class StockRoutes : IRouteProvider
{
    public IReadOnlyList<RouteConfig> GetRoutes() => new[]
    {
        new RouteConfig
        {
            RouteId = "stock-api-catchall",
            ClusterId = "stock-cluster",
            Order = 3,
            Match = new RouteMatch { Path = "/stock-api/{**catch-all}" },
            // Transforms = new[]
            // {
            //     new Dictionary<string, string> { ["PathRemovePrefix"] = "/order-api" },
            //     new Dictionary<string, string> { ["RequestHeaderOriginalHost"] = "true" }
            // }
            // AuthorizationPolicy = "DefaultPolicy"
        }
    };
}