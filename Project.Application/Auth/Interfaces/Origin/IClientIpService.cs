using System.Net;

namespace Project.Application.Auth.Interfaces.Origin;

/// <summary>Defines how the client's IP address is identified from an HTTP request.</summary>
public interface IClientIpService
{
    /// <summary>Gets the client's IP address, taking trusted proxies into account.</summary>
    /// <returns>The client's IP address, or <see langword="null"/> if it cannot be determined.</returns>
    IPAddress? GetClientIpAddress();
}
