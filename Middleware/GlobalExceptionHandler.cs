using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Middleware;

/// <summary>
/// Fallback handler dla wszystkich nieobsłużonych wyjątków (inne niż ServiceException).
/// Zawsze zwraca 500 ProblemDetails. Rejestrowany jako drugi, po ServiceExceptionHandler.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env    = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        _logger.LogError(exception, "Nieobsłużony wyjątek: {Method} {Path}",
            context.Request.Method, context.Request.Path);

        var pd = new ProblemDetails
        {
            Status   = StatusCodes.Status500InternalServerError,
            Title    = "Wewnętrzny błąd serwera.",
            Detail   = _env.IsDevelopment() ? exception.Message : null,
            Instance = $"{context.Request.Method} {context.Request.Path}",
        };
        pd.Extensions["requestId"] = context.TraceIdentifier;

        context.Response.StatusCode  = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(pd, ct);
        return true;
    }
}
