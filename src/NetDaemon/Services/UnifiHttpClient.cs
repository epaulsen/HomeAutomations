using System.Net.Http.Json;
using HomeAutomations.Models;
using Microsoft.Extensions.Logging;

namespace HomeAutomations.Services;

public interface IUnifiClient
{
    Task<UnifiResponse<ClientDevice>> GetDevicesAsync(CancellationToken cancellationToken = default);
}

public class UnifiHttpClient(HttpClient client, ILogger<UnifiHttpClient> logger) : IUnifiClient
{
    private static Guid? _siteId = null;


    public async Task<UnifiResponse<ClientDevice>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var siteId = await GetSiteId(client, logger);

        // TODO: If we hit 200 device limits we would need pagination here.
        var response =
            await client.GetFromJsonAsync<UnifiResponse<ClientDevice>>($"v1/sites/{siteId:D}/clients?limit=200");

        return response;
    }

    private static async Task<Guid> GetSiteId(HttpClient client, ILogger logger)
    {
        if (_siteId.HasValue)
        {
            return _siteId.Value;
        }

        var response = await client.GetFromJsonAsync<UnifiResponse<SiteDto>>($"v1/sites");
        Guid siteId = response.Data.Single().Id;
        logger.LogInformation("Unifi returned siteId '{siteId}'", siteId);
        return siteId;
    }
}