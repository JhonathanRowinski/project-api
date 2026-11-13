using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application.Auth.Interfaces;
using Project.Application.Auth.Interfaces.Origin;
using Project.Application.Auth.Interfaces.Repositories;
using Project.Application.Auth.Interfaces.Tokens;
using Project.Application.Auth.Models;
using Project.Application.Auth.Options;
using Project.Application.Exceptions;
using Project.Domain.Entities.Accounts;
using Project.Domain.Entities.Auth;

namespace Project.Application.Auth;

/// <summary>
/// Handles the authentication process, from validating credentials to creating,
/// refreshing, and revoking user sessions.
/// </summary>
/// <param name="accountRepository">Finds the account associated with the submitted credentials.</param>
/// <param name="authSessionRepository">Stores authentication sessions and keeps track of their status.</param>
/// <param name="passwordHasher">Checks whether the submitted password matches the stored password hash.</param>
/// <param name="jwtTokenGenerator">Creates the access tokens used to authenticate API requests.</param>
/// <param name="refreshTokenGenerator">Creates refresh tokens and generates secure hashes for storage.</param>
/// <param name="clientIpService">Identifies the client's IP address, taking trusted proxies into account.</param>
/// <param name="ipGeolocationService">Uses the client's IP address to retrieve approximate location information.</param>
/// <param name="httpRequestInformationService">Reads relevant information from the current HTTP request.</param>
/// <param name="userAgentParser">Identifies the browser, operating system, and device from the User-Agent string.</param>
/// <param name="options">Provides the configured session and refresh token lifetimes.</param>
/// <param name="logger">Records unexpected errors that occur during authentication.</param>
public sealed class LoginService(
    IAccountRepository accountRepository,
    IAuthSessionRepository authSessionRepository,
    IPasswordHasher<Account> passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenGenerator refreshTokenGenerator,
    IClientIpService clientIpService,
    IIpGeolocationService ipGeolocationService,
    IHttpRequestInformationService httpRequestInformationService,
    IUserAgentParser userAgentParser,
    IOptions<AuthTokenOptions> options,
    ILogger<LoginService> logger) : ILoginService
{
    /// <summary>Checks whether the email address provided during login has a valid format.</summary>
    private static readonly EmailAddressAttribute EmailValidator = new();

    /// <summary>Holds the configured token and session lifetime settings.</summary>
    private readonly AuthTokenOptions _options = options.Value;

    /// <summary>
    /// Checks the account credentials, gathers information about the client,
    /// creates a new session, and returns the tokens needed to authenticate.
    /// </summary>
    /// <param name="email">The email address associated with the account.</param>
    /// <param name="password">The password provided by the client.</param>
    /// <returns>The access and refresh tokens issued for the new session, along with their expiration times.</returns>
    public async Task<AuthTokens> LoginAsync(string email, string password)
    {
        try
        {
            var formattedEmail = email?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(formattedEmail) || !EmailValidator.IsValid(formattedEmail))
                throw InvalidCredentialsException();

            if (string.IsNullOrEmpty(password))
                throw InvalidCredentialsException();

            var account = await accountRepository.FindByEmailAsync(formattedEmail);

            if (account is null)
                throw InvalidCredentialsException();

            var verificationResult = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);

            if (verificationResult is not PasswordVerificationResult.Success and not PasswordVerificationResult.SuccessRehashNeeded)
                throw InvalidCredentialsException();

            var sessionId = Guid.NewGuid();
            var refreshToken = refreshTokenGenerator.GenerateToken();

            var nowUtc = DateTime.UtcNow;
            var absoluteExpiresAtUtc = nowUtc.AddDays(_options.SessionLifetimeDays);
            var refreshTokenExpiresAtUtc = nowUtc.AddDays(_options.RefreshTokenLifetimeDays);

            // Collect origin information
            var userAgentString = httpRequestInformationService.GetUserAgent();
            var userAgent = userAgentParser.Parse(userAgentString);
            var clientIp = clientIpService.GetClientIpAddress();
            var geolocation = clientIp is not null
                ? ipGeolocationService.GetGeolocation(clientIp)
                : null;

            var session = new AuthSession(
                sessionId,
                account.Id,
                refreshTokenGenerator.HashToken(refreshToken),
                nowUtc,
                refreshTokenExpiresAtUtc,
                absoluteExpiresAtUtc,
                RevokedAtUtc: null,
                Country: geolocation?.Country,
                CountryCode: geolocation?.CountryCode,
                State: geolocation?.State,
                StateCode: geolocation?.StateCode,
                City: geolocation?.City,
                Browser: userAgent?.BrowserName,
                BrowserVersion: userAgent?.BrowserVersion,
                OperatingSystem: userAgent?.OperatingSystemName,
                OperatingSystemVersion: userAgent?.OperatingSystemVersion,
                DeviceType: userAgent?.DeviceType);

            var accessToken = jwtTokenGenerator.GenerateAccessToken(account, sessionId, absoluteExpiresAtUtc);

            await authSessionRepository.AddAsync(session);

            return new AuthTokens(accessToken, refreshToken, refreshTokenExpiresAtUtc);
        }
        catch (Exception exception) when (exception is not AppException)
        {
            logger.LogError(exception, "Unexpected error while processing login.");

            throw new AppException(
                HttpStatusCode.InternalServerError,
                "Não foi possível concluir o login.",
                "Tente novamente mais tarde.",
                exception);
        }
    }

    /// <summary>
    /// Checks whether the refresh token is still valid, replaces it with a new one,
    /// and issues another access token without extending the session's absolute lifetime.
    /// </summary>
    /// <param name="refreshToken">The refresh token currently held by the client.</param>
    /// <returns>A new access token and refresh token, together with their expiration times.</returns>
    public async Task<AuthTokens> RefreshAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw InvalidSessionException();

            var currentRefreshTokenHash = refreshTokenGenerator.HashToken(refreshToken);
            var session = await authSessionRepository.FindByRefreshTokenHashAsync(currentRefreshTokenHash);
            var nowUtc = DateTime.UtcNow;

            if (session is null || !session.IsActive(nowUtc))
                throw InvalidSessionException();

            var account = await accountRepository.FindByIdAsync(session.AccountId);

            if (account is null)
                throw InvalidSessionException();

            var newRefreshToken = refreshTokenGenerator.GenerateToken();
            var newRefreshTokenHash = refreshTokenGenerator.HashToken(newRefreshToken);

            var slidingExpiresAtUtc = nowUtc.AddDays(_options.RefreshTokenLifetimeDays);

            var newRefreshTokenExpiresAtUtc = slidingExpiresAtUtc < session.AbsoluteExpiresAtUtc
                ? slidingExpiresAtUtc
                : session.AbsoluteExpiresAtUtc;

            var rotated = await authSessionRepository.RotateRefreshTokenAsync(
                currentRefreshTokenHash,
                newRefreshTokenHash,
                nowUtc,
                newRefreshTokenExpiresAtUtc);

            if (!rotated)
                throw InvalidSessionException();

            var accessToken = jwtTokenGenerator.GenerateAccessToken(account, session.Id, session.AbsoluteExpiresAtUtc);

            return new AuthTokens(accessToken, newRefreshToken, newRefreshTokenExpiresAtUtc);
        }
        catch (Exception exception) when (exception is not AppException)
        {
            logger.LogError(exception, "Unexpected error while refreshing an authentication session.");

            throw new AppException(
                HttpStatusCode.InternalServerError,
                "Não foi possível atualizar a sessão.",
                "Tente novamente mais tarde.",
                exception);
        }
    }

    /// <summary>Ends an authentication session using its identifier.</summary>
    /// <param name="sessionIdValue">The session identifier extracted from the authenticated user's access token.</param>
    /// <returns>A task that represents the session revocation operation.</returns>
    public async Task RevokeSessionAsync(string? sessionIdValue)
    {
        try
        {
            if (!Guid.TryParse(sessionIdValue, out var sessionId))
                throw InvalidSessionException();

            var revoked = await authSessionRepository.RevokeBySessionIdAsync(sessionId, DateTime.UtcNow);

            if (!revoked)
                throw InvalidSessionException();
        }
        catch (Exception exception) when (exception is not AppException)
        {
            logger.LogError(exception, "Unexpected error while revoking an authentication session.");

            throw new AppException(
                HttpStatusCode.InternalServerError,
                "Não foi possível encerrar a sessão.",
                "Tente novamente mais tarde.",
                exception);
        }
    }

    /// <summary>Creates the error returned when the submitted account credentials are invalid.</summary>
    /// <returns>An unauthorized application exception describing the authentication failure.</returns>
    private static AppException InvalidCredentialsException() => new(
        HttpStatusCode.Unauthorized,
        "Credenciais inválidas.",
        "Verifique seus dados e tente novamente.");

    /// <summary>Creates the error returned when a session cannot be used because it is invalid or expired.</summary>
    /// <returns>An unauthorized application exception describing the session failure.</returns>
    private static AppException InvalidSessionException() => new(
        HttpStatusCode.Unauthorized,
        "Sessão inválida ou expirada.",
        "Faça login novamente para continuar.");
}
