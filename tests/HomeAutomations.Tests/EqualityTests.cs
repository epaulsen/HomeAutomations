using System.Text.Json;
using HomeAutomations.Models;

namespace HomeAutomations.Tests;

public class EqualityTests
{
    [Fact]
    public void EqualToCopy()
    {
        var device1 = new ClientDevice()
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Access = new AccessInfo() { Type = "Default" },
            ConnectedAt = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            IpAddress = "1.2.3.4",
            MacAddress = "UncleMac",
            Type = "WIRED",
            UplinkDeviceId = Guid.NewGuid().ToString()
        };

        string json = JsonSerializer.Serialize(device1);
        var device2 = JsonSerializer.Deserialize<ClientDevice>(json);
        Assert.True(device1.Equals(device2));
    }
}