using ECommerce.YARP.Flags;
using Yarp.ReverseProxy.Configuration;

namespace ECommerce.YARP.Routing.Notification;

public class NotificationRoutes : IRouteProvider
{
    public IReadOnlyList<RouteConfig> GetRoutes() => new[]
    {
        new RouteConfig
        {
            RouteId = "notification-api-catchall",
            ClusterId = "notification-cluster",
            Order = 3,
            Match = new RouteMatch { Path = "/notification-api/{**catch-all}" },
            // Transforms = new[]
            // {
            //     new Dictionary<string, string> { ["PathRemovePrefix"] = "/order-api" },
            //     new Dictionary<string, string> { ["RequestHeaderOriginalHost"] = "true" }
            // }
            // AuthorizationPolicy = "DefaultPolicy"
        }
    };
}