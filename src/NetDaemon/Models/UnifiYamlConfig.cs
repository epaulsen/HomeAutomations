using System.Net;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace HomeAutomations.Models;

public class UnifiYamlConfig
{
    public List<NetworkConfig> Networks { get; set; } = new();

    public List<DeviceTrackerConfig> Trackers { get; set; } = new();
}

public class DeviceTrackerConfig
{
    public required string Name { get; set; }
    public required string MacAddress { get; set; }

    [YamlIgnore]
    public string UniqueId
    {
        get
        {
            var sanitizedName = Regex.Replace(Name, "[^a-zA-Z0-9]+", "_");
            return $"device_tracker.unifi_{sanitizedName}".ToLowerInvariant();
        }
    }
}
public class NetworkConfig
{
    /// <summary>
    /// Network name.  Will be used to create HA sensors
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// VLAN spec.  Must be on the form 10.100.5.0/24
    /// where 10.100.5.0 is the first IP address in the network, and /24 is the size
    /// </summary>
    public required string Vlan { get; set; }

    [YamlIgnore]
    public string UniqueId
    {
        get
        {
            var sanitizedName = Regex.Replace(Name, "[^a-zA-Z0-9]+", "_");
            return $"sensor.unifi_vlan_{sanitizedName}_device_count".ToLowerInvariant();
        }
    }

}