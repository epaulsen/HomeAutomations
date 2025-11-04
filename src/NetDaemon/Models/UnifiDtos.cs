namespace HomeAutomations.Models;

using System.Text.Json.Serialization;
using System.Collections.Generic;

public class UnifiResponse<T>
{
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new List<T>();
}

public class SiteDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("internalReference")]
    public string InternalReference { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}


public class ClientDevice : IEquatable<ClientDevice>, IComparable<ClientDevice>
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("connectedAt")]
    public DateTime ConnectedAt { get; set; }

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("access")]
    public AccessInfo? Access { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("macAddress")]
    public string? MacAddress { get; set; }

    [JsonPropertyName("uplinkDeviceId")]
    public string? UplinkDeviceId { get; set; }

    // Sammenligning for sortering (f.eks. på Id)
    public int CompareTo(ClientDevice? other)
    {
        if (other == null) return 1;
        return Id.CompareTo(other.Id);
    }

    // Likhetssjekk basert på alle relevante felter
    public bool Equals(ClientDevice? other)
    {
        if (other == null) return false;

        return Id == other.Id &&
               Name == other.Name &&
               IpAddress == other.IpAddress &&
               Type == other.Type &&
               MacAddress == other.MacAddress &&
               ((Access == null && other.Access == null) || (Access?.Equals(other.Access) ?? false));
    }

    public override bool Equals(object? obj) => Equals(obj as ClientDevice);

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, IpAddress, Type, MacAddress);
    }
}

public class AccessInfo :IEquatable<AccessInfo>, IEqualityComparer<AccessInfo>
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    public bool Equals(AccessInfo? other)
    {
        return other?.Type == Type;
    }

    public bool Equals(AccessInfo? x, AccessInfo? y)
    {
        return x?.Type == y?.Type;
    }

    public int GetHashCode(AccessInfo obj)
    {
        return obj.Type?.GetHashCode() ?? 0;
    }
}
