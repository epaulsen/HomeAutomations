using HomeAutomations.Apps.CostSensor;
using HomeAutomations.Apps.NordPoolApp;
using HomeAutomations.Hosts;
using HomeAutomations.Models;
using HomeAutomations.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetDaemon.AppModel;
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
                .AddSingleton<NordPoolSensor>()
                .AddSingleton<NordPoolSubsidizedSensor>()
                .AddHostedService<NordPoolBackgroundService>()
                .AddSingleton<NordPoolDataStorage>()
                .AddHttpClient<INordpoolApiClient, NordpoolApiClient>()
                .ConfigureHttpClient((sp, client) =>
                {
                    client.BaseAddress = new Uri(NordpoolApiClient.BaseUrl);
                }).Services
                .AddNetDaemonStateManager()
                .AddNetDaemonScheduler()
                //.AddNetDaemonApp<CostSensorApp>();
                .AddNetDaemonApp<NordPoolSensorApp>();
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

