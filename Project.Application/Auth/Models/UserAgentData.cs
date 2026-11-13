namespace Project.Application.Auth.Models;

/// <summary>Contains browser, operating system, and device information identified from a User-Agent string.</summary>
public sealed record UserAgentData
{
    /// <summary>The name of the browser.</summary>
    public string? BrowserName { get; init; }

    /// <summary>The browser version.</summary>
    public string? BrowserVersion { get; init; }

    /// <summary>The name of the operating system.</summary>
    public string? OperatingSystemName { get; init; }

    /// <summary>The operating system version.</summary>
    public string? OperatingSystemVersion { get; init; }

    /// <summary>The type of device, such as desktop, mobile, or tablet.</summary>
    public string? DeviceType { get; init; }
}
