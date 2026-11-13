using System.Net;

namespace Project.Application.Exceptions;

/// <summary>Represents an application error that carries an HTTP status code and optional details for the client.</summary>
/// <param name="statusCode">The HTTP status code to use in the response.</param>
/// <param name="message">A short description of the error.</param>
/// <param name="detail">Optional additional information that can safely be shared with the client.</param>
/// <param name="innerException">The original exception, if the error was caused by another exception.</param>
public sealed class AppException(
    HttpStatusCode statusCode,
    string message,
    string? detail = null,
    Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>The HTTP status code associated with this error.</summary>
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>Additional information that can safely be shared with the client, if available.</summary>
    public string? Detail { get; } = detail;
}
