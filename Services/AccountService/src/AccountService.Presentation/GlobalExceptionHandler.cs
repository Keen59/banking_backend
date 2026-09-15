using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace AccountService.Presentation;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, message, errors) = exception switch
        {
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                exception.Message,
                Array.Empty<string>()),
            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                exception.Message,
                Array.Empty<string>()),
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Doğrulama hatası.",
                validationException.Errors.Select(error => error.ErrorMessage).ToArray()),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Beklenmeyen bir hata oluştu.",
                Array.Empty<string>())
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            isSuccess = false,
            message,
            errors,
            traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier
        }, cancellationToken);

        return true;
    }
}
