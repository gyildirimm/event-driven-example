using ECommerce.YARP.Flags;
using Yarp.ReverseProxy.Configuration;

namespace ECommerce.YARP.Routing.Stock;

public class StockClusterProvider(IConfiguration config) : IClusterProvider
{
    private readonly IConfiguration _config = config;

    public IReadOnlyList<ClusterConfig> GetClusters() => new[]
    {
        new ClusterConfig
        {
            ClusterId = "stock-cluster",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["stock-api"] = new DestinationConfig
                {
                    Address = _config.GetValue<string>("Endpoints:StockAPI") ?? string.Empty
                }
            }
        }
    };
}