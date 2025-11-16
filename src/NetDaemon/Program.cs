using HomeAutomations.Apps.CostSensor;
using HomeAutomations.Apps.NordPoolApp;
using HomeAutomations.apps.UnifiApp;
using HomeAutomations.Hosts;
using HomeAutomations.Models;
using HomeAutomations.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
                .AddOptions<UnifiConfig>().BindConfiguration("Unifi")
                .Services
                .AddHttpClient<IUnifiClient, UnifiHttpClient>()
                .ConfigureHttpClient((sp, client) =>
                {
                    var config = sp.GetRequiredService<IOptions<UnifiConfig>>();
                    client.BaseAddress = new Uri(config.Value.BaseUrl, UriKind.Absolute);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                    client.DefaultRequestHeaders.Add("X-API-KEY", config.Value.ApiKey);
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };
                })
                .Services
                .AddSingleton(TimeProvider.System)
                .AddSingleton<UnifiData>()
                .AddHostedService<UnifiBackgroundService>()
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
                .AddNetDaemonApp<NetworkDeviceTrackerApp>()
                .AddNetDaemonApp<DeviceTrackerApp>()
                .AddNetDaemonApp<CostSensorApp>()
                .AddNetDaemonApp<NordPoolSensorApp>()
                ;

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

