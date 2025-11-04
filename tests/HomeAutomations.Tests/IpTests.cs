using System.Diagnostics;
using System.Net;
using HomeAutomations.Models;
using YamlDotNet.Serialization.NamingConventions;

namespace HomeAutomations.Tests;

public class IpTests
{
    [Fact]
    public void Serialize()
    {
        var uc = new UnifiYamlConfig()
        {
            Networks =
            [
                new NetworkConfig() { Name = "Default", Vlan = "192.168.1.0/24" },
                new NetworkConfig() { Name = "Guest", Vlan = "192.168.10.0/24" },
                new NetworkConfig() { Name = "IOT", Vlan = "192.168.55.0/24" },
            ],
            Trackers =
            [
                new DeviceTrackerConfig() {Name = "Erling iPhone", MacAddress = "3c:6d:89:86:ba:a6"}
            ]
        };

        var serializer = new YamlDotNet.Serialization.SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(uc);
        Debug.WriteLine(yaml);

    }
}