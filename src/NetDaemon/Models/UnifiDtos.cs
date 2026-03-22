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
    public string? InternalReference { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
}


public class ClientDevice : IEquatable<ClientDevice>, IComparable<ClientDevice>
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

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

    // Comparison for sorting (by all fields to be consistent with Equals)
    public int CompareTo(ClientDevice? other)
    {
        if (other == null) return 1;
        int cmp = Id.CompareTo(other.Id);
        if (cmp != 0) return cmp;
        cmp = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (cmp != 0) return cmp;
        cmp = string.Compare(IpAddress, other.IpAddress, StringComparison.Ordinal);
        if (cmp != 0) return cmp;
        cmp = string.Compare(Type, other.Type, StringComparison.Ordinal);
        if (cmp != 0) return cmp;
        cmp = string.Compare(MacAddress, other.MacAddress, StringComparison.Ordinal);
        if (cmp != 0) return cmp;
        return string.Compare(Access?.Type, other.Access?.Type, StringComparison.Ordinal);
    }

    // Equality check based on all relevant fields
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
        return HashCode.Combine(Id, Name, IpAddress, Type, MacAddress, Access?.Type);
    }
}

public class AccessInfo : IEquatable<AccessInfo>
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    public bool Equals(AccessInfo? other)
    {
        return other?.Type == Type;
    }

    public override bool Equals(object? obj) => Equals(obj as AccessInfo);

    public override int GetHashCode()
    {
        return Type?.GetHashCode() ?? 0;
    }
}
