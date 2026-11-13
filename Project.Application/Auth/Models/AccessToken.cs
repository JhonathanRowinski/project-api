namespace Project.Application.Auth.Models;

/// <summary>Represents a signed access token and the time it expires.</summary>
/// <param name="Value">The signed JWT string.</param>
/// <param name="ExpiresAtUtc">The date and time when the token expires, in UTC.</param>
public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
