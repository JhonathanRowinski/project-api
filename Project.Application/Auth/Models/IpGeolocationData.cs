namespace Project.Application.Auth.Models;

/// <summary>Contains approximate location information associated with an IP address.</summary>
public sealed record IpGeolocationData
{
    /// <summary>The country where the IP address is located.</summary>
    public string? Country { get; init; }

    /// <summary>The country's ISO code, such as "US" or "BR".</summary>
    public string? CountryCode { get; init; }

    /// <summary>The state or region where the IP address is located.</summary>
    public string? State { get; init; }

    /// <summary>The state or region code, such as "CA" or "SP".</summary>
    public string? StateCode { get; init; }

    /// <summary>The city associated with the IP address.</summary>
    public string? City { get; init; }
}
