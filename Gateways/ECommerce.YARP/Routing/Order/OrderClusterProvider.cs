using ECommerce.YARP.Flags;
using Yarp.ReverseProxy.Configuration;

namespace ECommerce.YARP.Routing.Order;

public class OrderClusterProvider(IConfiguration config) : IClusterProvider
{
    private readonly IConfiguration _config = config;

    public IReadOnlyList<ClusterConfig> GetClusters() => new[]
    {
        new ClusterConfig
        {
            ClusterId = "order-cluster",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["order-api"] = new DestinationConfig
                {
                    Address = _config.GetValue<string>("Endpoints:OrderAPI") ?? string.Empty
                }
            }
        }
    };
}