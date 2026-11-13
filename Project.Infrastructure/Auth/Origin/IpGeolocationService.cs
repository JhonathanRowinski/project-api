using System.Net;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application.Auth.Interfaces.Origin;
using Project.Application.Auth.Models;

namespace Project.Infrastructure.Auth.Origin;

/// <summary>Defines the settings used to locate the GeoLite2 City database.</summary>
public sealed class IpGeolocationOptions
{
    /// <summary>The configuration section used to load these settings.</summary>
    public const string SectionName = "IpGeolocation";

    /// <summary>The path to the GeoLite2 City database file (.mmdb).</summary>
    public string? GeoLite2DatabasePath { get; init; }
}

/// <summary>Looks up approximate location information for IP addresses using the MaxMind GeoLite2 City database.</summary>
public sealed class IpGeolocationService : IIpGeolocationService, IDisposable
{
    private readonly ILogger<IpGeolocationService> _logger;
    private DatabaseReader? _reader;
    private readonly object _lock = new();

    /// <summary>Creates the service and attempts to load the GeoLite2 database from the configured path.</summary>
    public IpGeolocationService(IOptions<IpGeolocationOptions> options, ILogger<IpGeolocationService> logger)
    {
        _logger = logger;

        var dbPath = options.Value.GeoLite2DatabasePath;
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            _logger.LogWarning("GeoLite2 database path not configured. IP geolocation will be unavailable.");
            return;
        }

        try
        {
            if (!File.Exists(dbPath))
            {
                _logger.LogWarning("GeoLite2 database file not found at {DatabasePath}. IP geolocation will be unavailable.", dbPath);
                return;
            }

            lock (_lock)
            {
                _reader = new DatabaseReader(dbPath);
            }

            _logger.LogInformation("GeoLite2 database loaded from {DatabasePath}.", dbPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load GeoLite2 database from {DatabasePath}. IP geolocation will be unavailable.", dbPath);
        }
    }

    /// <summary>Looks up the approximate location associated with an IP address.</summary>
    public IpGeolocationData? GetGeolocation(IPAddress ipAddress)
    {
        if (ipAddress is null)
            return null;

        // Reject private and reserved IP addresses
        if (IsPrivateOrReserved(ipAddress))
            return null;

        lock (_lock)
        {
            if (_reader is null)
                return null;

            try
            {
                var response = _reader.City(ipAddress);

                return new IpGeolocationData
                {
                    Country = response.Country.Name,
                    CountryCode = response.Country.IsoCode,
                    State = response.MostSpecificSubdivision.Name,
                    StateCode = response.MostSpecificSubdivision.IsoCode,
                    City = response.City.Name
                };
            }
            catch (AddressNotFoundException)
            {
                // IP not found in database, return null
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error geolocating IP address {IpAddress}.", ipAddress);
                return null;
            }
        }
    }

    /// <summary>Checks whether an IP address belongs to a private or reserved range.</summary>
    private static bool IsPrivateOrReserved(IPAddress ip)
    {
        // IPv4 private ranges: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, 127.0.0.0/8, 0.0.0.0/8, 224.0.0.0/4, 240.0.0.0/4
        // IPv6 private ranges: fc00::/7, fe80::/10, ::1/128, ::/128
        var bytes = ip.GetAddressBytes();

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            // Loopback
            if (bytes[0] == 127)
                return true;
            // Private
            if (bytes[0] == 10)
                return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;
            // This host/Unspecified
            if (bytes[0] == 0)
                return true;
            // Multicast
            if (bytes[0] >= 224 && bytes[0] < 240)
                return true;
            // Reserved
            if (bytes[0] >= 240)
                return true;
        }
        else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // Loopback
            if (ip == IPAddress.IPv6Loopback)
                return true;
            // Unspecified
            if (ip == IPAddress.IPv6None)
                return true;
            // Link-local (fe80::/10)
            if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
                return true;
            // Unique local (fc00::/7)
            if ((bytes[0] & 0xfe) == 0xfc)
                return true;
            // Multicast (ff00::/8)
            if (bytes[0] == 0xff)
                return true;
        }

        return false;
    }

    /// <summary>Releases the resources held by the database reader.</summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _reader?.Dispose();
        }
    }
}
