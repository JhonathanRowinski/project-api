using System.Net;
using Project.Application.Auth.Models;

namespace Project.Application.Auth.Interfaces.Origin;

/// <summary>Provides approximate location information based on client IP addresses.</summary>
public interface IIpGeolocationService
{
    /// <summary>Looks up the approximate location associated with an IP address.</summary>
    /// <param name="ipAddress">The IP address to look up.</param>
    /// <returns>The available location information, or <see langword="null"/> if no data can be found.</returns>
    IpGeolocationData? GetGeolocation(IPAddress ipAddress);
}
