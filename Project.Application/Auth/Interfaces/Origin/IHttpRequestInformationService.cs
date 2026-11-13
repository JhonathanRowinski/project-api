namespace Project.Application.Auth.Interfaces.Origin;

/// <summary>Provides access to relevant information from the current HTTP request.</summary>
public interface IHttpRequestInformationService
{
    /// <summary>Reads the User-Agent header sent by the client.</summary>
    /// <returns>The User-Agent string, or <see langword="null"/> if the header is unavailable.</returns>
    string? GetUserAgent();
}
