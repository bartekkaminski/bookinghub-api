using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace BookingHub.Api.Middleware;

/// <summary>
/// Middleware weryfikujące, czy aktualnie zalogowany użytkownik ma aktywne konto.
///
/// Działa niezależnie od czasu życia tokenu JWT — blokuje dostęp natychmiast po dezaktywacji konta.
/// Pobrana encja User jest zapisywana w HttpContext.Items[CurrentUserService.CurrentUserKey],
/// dzięki czemu CurrentUserService może odczytać dane synchronicznie bez dodatkowego zapytania do bazy.
///
/// Wynik jest cachowany w IMemoryCache (klucz: externalId, TTL: 60 s).
///
/// Kolejność w pipeline: po UseAuthentication(), przed UseAuthorization().
/// </summary>
public sealed class ActiveUserMiddleware
{
    private const int    CacheTtlSeconds = 60;
    private const string CacheKeyPrefix  = "bookinghub:active_user:";

    private readonly RequestDelegate             _next;
    private readonly IMemoryCache                _cache;
    private readonly ILogger<ActiveUserMiddleware> _logger;

    public ActiveUserMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<ActiveUserMiddleware> logger)
    {
        _next   = next;
        _cache  = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        // Żądania nieautentykowane przepuszczamy — JWT middleware zwróci 401 samodzielnie.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var externalId =
            context.User.FindFirst("sub")?.Value ??
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(externalId))
        {
            await _next(context);
            return;
        }

        var cacheKey = CacheKeyPrefix + externalId;

        if (!_cache.TryGetValue(cacheKey, out User? user))
        {
            user = await userRepository.GetByExternalIdIgnoreFiltersAsync(
                externalId, "kinde", context.RequestAborted);

            // Cachujemy tylko znalezionych użytkowników.
            // Null = użytkownik nie istnieje jeszcze w bazie (trwa provisioning przez POST /api/auth/me).
            // Cachowanie null blokowałoby przez 60s wszystkie żądania tuż po rejestracji.
            if (user is not null)
                _cache.Set(cacheKey, user, TimeSpan.FromSeconds(CacheTtlSeconds));
        }

        // Blokujemy tylko konta jawnie dezaktywowane lub soft-deleted.
        // Nieznany użytkownik (null) jest przepuszczany — może dopiero tworzyć konto przez POST /api/auth/me.
        if (user is not null && (!user.IsActive || user.IsDeleted))
        {
            _logger.LogWarning(
                "Zablokowano żądanie od nieaktywnego użytkownika {ExternalId} [{Path}].",
                externalId, context.Request.Path);

            context.Response.StatusCode  = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status   = StatusCodes.Status403Forbidden,
                Title    = "Konto dezaktywowane.",
                Detail   = "Twoje konto zostało dezaktywowane. Skontaktuj się z administratorem.",
                Extensions = { ["errorCode"] = "AccountInactive" },
            }, context.RequestAborted);
            return;
        }

        // Udostępniamy encję dla CurrentUserService — bez drugiego zapytania do bazy.
        if (user is not null)
            context.Items[CurrentUserService.CurrentUserKey] = user;

        await _next(context);
    }

    /// <summary>
    /// Natychmiast unieważnia wpis cache dla danego użytkownika po zmianie IsActive.
    /// Wywołaj z UserService po operacji dezaktywacji/reaktywacji konta.
    /// </summary>
    public static void InvalidateCache(IMemoryCache cache, string externalId)
        => cache.Remove(CacheKeyPrefix + externalId);
}
