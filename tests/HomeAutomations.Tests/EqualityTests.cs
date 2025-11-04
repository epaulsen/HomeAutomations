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
            ConnectedAt = DateTime.Now,
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