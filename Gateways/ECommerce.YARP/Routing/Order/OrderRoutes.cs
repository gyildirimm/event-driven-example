using ECommerce.YARP.Flags;
using Yarp.ReverseProxy.Configuration;

namespace ECommerce.YARP.Routing.Order;

public class OrderRoutes : IRouteProvider
{
    public IReadOnlyList<RouteConfig> GetRoutes() => new[]
    {
        new RouteConfig
        {
            RouteId = "order-api-catchall",
            ClusterId = "order-cluster",
            Order = 3,
            Match = new RouteMatch { Path = "/order-api/{**catch-all}" },
            // Transforms = new[]
            // {
            //     new Dictionary<string, string> { ["PathRemovePrefix"] = "/order-api" },
            //     new Dictionary<string, string> { ["RequestHeaderOriginalHost"] = "true" }
            // }
            // AuthorizationPolicy = "DefaultPolicy"
        }
    };
}