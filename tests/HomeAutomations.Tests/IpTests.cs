using HomeAutomations.Models;
using YamlDotNet.Serialization;
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
                new DeviceTrackerConfig() { Name = "Erling iPhone", MacAddress = "3c:6d:89:86:ba:a6" }
            ]
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        var yaml = serializer.Serialize(uc);

        Assert.Contains("name: Default", yaml);
        Assert.Contains("vlan: 192.168.1.0/24", yaml);
        Assert.Contains("mac_address: 3c:6d:89:86:ba:a6", yaml);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        var deserialized = deserializer.Deserialize<UnifiYamlConfig>(yaml);

        Assert.Equal(3, deserialized.Networks.Count);
        Assert.Equal("Default", deserialized.Networks[0].Name);
        Assert.Equal("3c:6d:89:86:ba:a6", deserialized.Trackers[0].MacAddress);
    }
}