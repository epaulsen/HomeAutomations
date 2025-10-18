using HomeAutomations.Apps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetDaemon.AppModel;
using NetDaemon.Extensions.Logging;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.Runtime;
using Serilog;

try
{
    await Host.CreateDefaultBuilder(args)
        .UseNetDaemonAppSettings()
        .UseNetDaemonRuntime()
        .UseNetDaemonMqttEntityManagement()
        .ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.Development.json", optional: true);
        })
        .ConfigureServices((_, services) =>
        {
            services
                .AddNetDaemonStateManager()
                .AddNetDaemonScheduler()
                .AddNetDaemonApp<CostSensorApp>();
        })
        .UseSerilog((context, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console();
        })
        .Build()
        .RunAsync();
}
catch (Exception e)
{
    Console.WriteLine($"Failed to start host... {e}");
    throw;
}

