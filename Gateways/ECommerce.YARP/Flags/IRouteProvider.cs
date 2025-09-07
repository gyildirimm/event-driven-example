using Yarp.ReverseProxy.Configuration;

namespace ECommerce.YARP.Flags;

public interface IRouteProvider
{
    IReadOnlyList<RouteConfig> GetRoutes();
}