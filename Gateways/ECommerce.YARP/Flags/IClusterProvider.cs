using Yarp.ReverseProxy.Configuration;

namespace ECommerce.YARP.Flags;

public interface IClusterProvider
{
    IReadOnlyList<ClusterConfig> GetClusters();
}