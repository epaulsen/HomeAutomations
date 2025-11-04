namespace HomeAutomations.Extensions;

public class ConfigFolder
{
    public static bool IsRunningInContainer
    {
        get
        {
            // Check if running in a container
            var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            return bool.TryParse(runningInContainer, out var isContainer) && isContainer;
        }
    }

    public static string Path
    {
        get
        {
            if (IsRunningInContainer)
            {
                return "/config";
            }

            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps", "config");
        }
    }
}