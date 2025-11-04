namespace HomeAutomations.Models;

public class UnifiConfig
{
    public string BaseUrl { get; set; } = "localhost";
    public string ApiKey { get; set; } = "";
    public int PollIntervalSeconds { get; set; } = 5;
}