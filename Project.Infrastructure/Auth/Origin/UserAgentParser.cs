using DeviceDetectorNET;
using Microsoft.Extensions.Logging;
using Project.Application.Auth.Interfaces.Origin;
using Project.Application.Auth.Models;

namespace Project.Infrastructure.Auth.Origin;

/// <summary>Extracts browser, operating system, and device information from User-Agent strings using DeviceDetector.NET.</summary>
public sealed class UserAgentParser : IUserAgentParser
{
    private readonly ILogger<UserAgentParser> _logger;

    /// <summary>Creates the parser with the logger used to record parsing errors.</summary>
    public UserAgentParser(ILogger<UserAgentParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Identifies the browser, operating system, and device from a User-Agent string.
    /// Bot requests and strings that cannot be parsed are ignored.
    /// </summary>
    /// <param name="userAgentString">The User-Agent value received in the HTTP request.</param>
    /// <returns>The extracted client information, or <see langword="null"/> if the value is empty, belongs to a bot, or cannot be parsed.</returns>
    public UserAgentData? Parse(string? userAgentString)
    {
        if (string.IsNullOrWhiteSpace(userAgentString))
            return null;

        try
        {
            var detector = new DeviceDetector(userAgentString);
            detector.Parse();

            if (detector.IsBot())
                return null;

            var browser = detector.GetBrowserClient();
            var os = detector.GetOs();

            return new UserAgentData
            {
                BrowserName = browser?.Match.Name,
                BrowserVersion = browser?.Match.Version,
                OperatingSystemName = os?.Match.Name,
                OperatingSystemVersion = os?.Match.Version,
                DeviceType = detector.GetDeviceName()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception parsing User-Agent string: {UserAgent}", userAgentString);
            return null;
        }
    }
}
