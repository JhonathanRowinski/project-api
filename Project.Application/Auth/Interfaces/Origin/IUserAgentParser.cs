using Project.Application.Auth.Models;

namespace Project.Application.Auth.Interfaces.Origin;

/// <summary>Defines how client information is extracted from User-Agent strings.</summary>
public interface IUserAgentParser
{
    /// <summary>Identifies the browser, operating system, and device from a User-Agent string.</summary>
    /// <param name="userAgentString">The User-Agent value received in the HTTP request.</param>
    /// <returns>The extracted client information, or <see langword="null"/> if it cannot be identified.</returns>
    UserAgentData? Parse(string? userAgentString);
}
