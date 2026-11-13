using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application.Auth.Interfaces.Origin;

namespace Project.Infrastructure.Auth.Origin;

/// <summary>Defines which proxy addresses are trusted when identifying the client's IP address.</summary>
public sealed class ClientIpOptions
{
    /// <summary>The configuration section used to load these settings.</summary>
    public const string SectionName = "ClientIp";

    /// <summary>The IP addresses and CIDR ranges allowed to forward client IP information.</summary>
    public string[]? TrustedProxies { get; init; }
}

/// <summary>Identifies the client's IP address while accounting for trusted proxies.</summary>
public sealed class ClientIpService : IClientIpService
{
    private readonly ClientIpOptions _options;
    private readonly ILogger<ClientIpService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HashSet<string> _trustedProxySet;
    private readonly List<(string Network, IPAddress BaseAddress, int PrefixLength, bool IsIpv6)> _trustedNetworks;

    /// <summary>Creates the service and loads the trusted proxy configuration.</summary>
    public ClientIpService(
        IOptions<ClientIpOptions> options,
        ILogger<ClientIpService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _options = options.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _trustedProxySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _trustedNetworks = new List<(string, IPAddress, int, bool)>();

        ParseTrustedProxies();
    }

    /// <summary>Gets the client's IP address from the current HTTP request, using forwarded headers only when the remote address is trusted.</summary>
    public IPAddress? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Connection?.RemoteIpAddress is null)
            return null;

        var remoteIp = httpContext.Connection.RemoteIpAddress;

        // If remote IP is not in trusted proxies, ignore X-Forwarded-For completely
        if (!IsTrustedProxy(remoteIp))
            return remoteIp;

        // Remote is trusted, check X-Forwarded-For header
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(forwardedFor))
            return remoteIp;

        // Parse the X-Forwarded-For chain from right to left
        var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Reverse().ToList();

        // Start from the rightmost (closest to us) and work backwards until we find a non-trusted IP
        foreach (var ip in ips)
        {
            if (!IPAddress.TryParse(ip, out var parsedIp))
                continue;

            // If this IP is not trusted, it's the client IP
            if (!IsTrustedProxy(parsedIp))
                return parsedIp;
        }

        // All IPs in the chain are trusted, return the leftmost (original client)
        if (IPAddress.TryParse(ips.Last(), out var clientIp))
            return clientIp;

        // Fallback to remote IP
        return remoteIp;
    }

    /// <summary>Checks whether an IP address matches a trusted proxy address or network.</summary>
    private bool IsTrustedProxy(IPAddress ip)
    {
        // Check exact match first (faster)
        if (_trustedProxySet.Contains(ip.ToString()))
            return true;

        // Check CIDR ranges
        foreach (var (network, baseAddress, prefixLength, isIpv6) in _trustedNetworks)
        {
            var addressFamily = ip.AddressFamily;
            if ((isIpv6 && addressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) ||
                (!isIpv6 && addressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            {
                if (IsIpInNetwork(ip, baseAddress, prefixLength))
                    return true;
            }
        }

        return false;
    }

    /// <summary>Checks whether an IP address falls within the specified CIDR network.</summary>
    private bool IsIpInNetwork(IPAddress ip, IPAddress networkBase, int prefixLength)
    {
        var ipBytes = ip.GetAddressBytes();
        var networkBytes = networkBase.GetAddressBytes();

        if (ipBytes.Length != networkBytes.Length)
            return false;

        int byteCount = prefixLength / 8;
        int bitCount = prefixLength % 8;

        // Compare full bytes
        for (int i = 0; i < byteCount; i++)
        {
            if (ipBytes[i] != networkBytes[i])
                return false;
        }

        // Compare remaining bits in the partial byte
        if (bitCount > 0)
        {
            byte mask = (byte)(0xFF << (8 - bitCount));
            if ((ipBytes[byteCount] & mask) != (networkBytes[byteCount] & mask))
                return false;
        }

        return true;
    }

    /// <summary>Reads the trusted proxy settings and stores valid IP addresses and CIDR ranges.</summary>
    private void ParseTrustedProxies()
    {
        if (_options.TrustedProxies is null || _options.TrustedProxies.Length == 0)
            return;

        foreach (var proxy in _options.TrustedProxies)
        {
            if (string.IsNullOrWhiteSpace(proxy))
                continue;

            // Check if it's a CIDR range
            if (proxy.Contains('/'))
            {
                try
                {
                    var parts = proxy.Split('/');
                    if (parts.Length != 2 || !int.TryParse(parts[1], out int prefixLength))
                    {
                        _logger.LogWarning("Invalid CIDR range in trusted proxies: {Proxy}", proxy);
                        continue;
                    }

                    if (IPAddress.TryParse(parts[0], out var baseAddress))
                    {
                        var isIpv6 = baseAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
                        int maxPrefix = isIpv6 ? 128 : 32;

                        if (prefixLength < 0 || prefixLength > maxPrefix)
                        {
                            _logger.LogWarning("Invalid prefix length {PrefixLength} in CIDR range: {Proxy}", prefixLength, proxy);
                            continue;
                        }

                        _trustedNetworks.Add((proxy, baseAddress, prefixLength, isIpv6));
                    }
                    else
                    {
                        _logger.LogWarning("Invalid CIDR base address in trusted proxies: {Proxy}", proxy);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing CIDR range in trusted proxies: {Proxy}", proxy);
                }
            }
            else
            {
                // Try to parse as a single IP address
                if (IPAddress.TryParse(proxy, out var ip))
                {
                    _trustedProxySet.Add(ip.ToString());
                }
                else
                {
                    _logger.LogWarning("Invalid IP address in trusted proxies: {Proxy}", proxy);
                }
            }
        }
    }
}
