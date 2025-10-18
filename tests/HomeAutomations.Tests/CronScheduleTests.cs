using HomeAutomations.Apps;
using Xunit;

namespace HomeAutomations.Tests;

public class CronScheduleTests
{
    [Theory]
    [InlineData(null, CronSchedule.None)]
    [InlineData("", CronSchedule.None)]
    [InlineData("  ", CronSchedule.None)]
    [InlineData("daily", CronSchedule.Daily)]
    [InlineData("Daily", CronSchedule.Daily)]
    [InlineData("DAILY", CronSchedule.Daily)]
    [InlineData("monthly", CronSchedule.Monthly)]
    [InlineData("Monthly", CronSchedule.Monthly)]
    [InlineData("MONTHLY", CronSchedule.Monthly)]
    [InlineData("yearly", CronSchedule.Yearly)]
    [InlineData("Yearly", CronSchedule.Yearly)]
    [InlineData("YEARLY", CronSchedule.Yearly)]
    [InlineData("invalid", CronSchedule.None)]
    [InlineData("weekly", CronSchedule.None)]
    public void CronSchedule_ParsesCorrectly(string? cronValue, CronSchedule expectedSchedule)
    {
        // Arrange
        var entry = new CostSensorEntry
        {
            Name = "Test Sensor",
            UniqueId = "sensor.test",
            Tariff = "sensor.tariff",
            Energy = "sensor.energy",
            Cron = cronValue
        };

        // Act
        var actualSchedule = entry.CronSchedule;

        // Assert
        Assert.Equal(expectedSchedule, actualSchedule);
    }

    [Fact]
    public void CronScheduleEnum_HasCorrectValues()
    {
        // Verify the enum has the expected values
        Assert.Equal(0, (int)CronSchedule.None);
        Assert.Equal(1, (int)CronSchedule.Daily);
        Assert.Equal(2, (int)CronSchedule.Monthly);
        Assert.Equal(3, (int)CronSchedule.Yearly);
    }
}
