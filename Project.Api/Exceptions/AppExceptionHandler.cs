using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Exceptions;

namespace Project.Api.Exceptions;

/// <summary>Converts application exceptions into consistent HTTP problem responses.</summary>
public sealed class AppExceptionHandler(ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>Handles an exception and writes its corresponding problem response.</summary>
    /// <param name="httpContext">The HTTP context of the current request.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="cancellationToken">A token used to cancel the response-writing operation.</param>
    /// <returns><see langword="true"/> if the exception was handled.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception is AppException appException
            ? ((int)appException.StatusCode, appException.Message, appException.Detail)
            : (StatusCodes.Status500InternalServerError, "Ocorreu um erro interno.", "Tente novamente mais tarde.");

        if (exception is not AppException)
            logger.LogError(exception, "Unhandled exception while processing the request.");

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        }, cancellationToken);

        return true;
    }
}