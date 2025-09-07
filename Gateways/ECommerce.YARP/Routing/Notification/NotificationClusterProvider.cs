using ECommerce.YARP.Flags;
using Yarp.ReverseProxy.Configuration;

namespace ECommerce.YARP.Routing.Notification;

public class NotificationClusterProvider(IConfiguration config) : IClusterProvider
{
    private readonly IConfiguration _config = config;

    public IReadOnlyList<ClusterConfig> GetClusters() => new[]
    {
        new ClusterConfig
        {
            ClusterId = "notification-cluster",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["notification-api"] = new DestinationConfig
                {
                    Address = _config.GetValue<string>("Endpoints:NotificationAPI") ?? string.Empty
                }
            }
        }
    };
}