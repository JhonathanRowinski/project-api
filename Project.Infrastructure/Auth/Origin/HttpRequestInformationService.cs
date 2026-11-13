using Microsoft.AspNetCore.Http;
using Project.Application.Auth.Interfaces.Origin;

namespace Project.Infrastructure.Auth.Origin;

/// <summary>Reads client information from the current HTTP request.</summary>
public sealed class HttpRequestInformationService : IHttpRequestInformationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates the service with access to the current HTTP context.</summary>
    public HttpRequestInformationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>Reads the User-Agent header from the current HTTP request, if available.</summary>
    /// <returns>The client's User-Agent string, or <see langword="null"/> if no HTTP context or header is available.</returns>
    public string? GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.FirstOrDefault();
    }
}
