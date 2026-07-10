using BookingHub.Api.Dtos.Auth;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Endpointy autoryzacji — wywoływane przez frontend po zalogowaniu przez Kinde.
/// Odpowiadają za auto-provisioning użytkownika oraz pobieranie profilu.
/// </summary>
[Route("api/auth")]
public sealed class AuthController : BookingHubControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Wywoływane przez React frontend tuż po zalogowaniu przez Kinde.
    /// Przy pierwszym logowaniu tworzy rekord User + Person w lokalnej bazie na podstawie claims JWT.
    /// Przy kolejnych logowaniach synchronizuje email, imię, nazwisko.
    /// Zwraca pełny profil z listą organizacji i ról — frontend używa tego do routingu.
    /// </summary>
    /// <remarks>
    /// Wymagane: token JWT z Kinde z claimem 'sub'. Opcjonalne: 'email', 'given_name', 'family_name'.
    /// </remarks>
    [HttpPost("me")]
    [ProducesResponseType(typeof(AuthMeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AuthMeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthMeResponse>> ProvisionMe(CancellationToken ct)
    {
        var sub       = User.FindFirstValue("sub");
        var email     = User.FindFirstValue("email");
        var firstName = User.FindFirstValue("given_name") ?? User.FindFirstValue("first_name");
        var lastName  = User.FindFirstValue("family_name") ?? User.FindFirstValue("last_name");

        if (string.IsNullOrWhiteSpace(sub))
            throw new ServiceException(ServiceErrorCode.Unauthorized,
                "Brak claimu 'sub' w tokenie JWT.");

        var isNewUser = await _userService.GetMeAsync(sub, ct) is null;

        var request = new ProvisionUserRequest
        {
            ExternalId   = sub,
            AuthProvider = "kinde",
            Email        = email,
            FirstName    = firstName,
            LastName      = lastName,
        };

        var profile = await _userService.ProvisionAsync(request, ct);

        // 201 przy pierwszym provisioning, 200 przy synchronizacji istniejącego konta.
        return isNewUser
            ? CreatedAtAction(nameof(GetMe), profile)
            : Ok(profile);
    }

    /// <summary>
    /// Zwraca profil aktualnie zalogowanego użytkownika — read-only, bez efektów ubocznych.
    /// Jeśli użytkownik nie istnieje w bazie — 404. Wywołaj POST /api/auth/me aby zainicjować konto.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthMeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthMeResponse>> GetMe(CancellationToken ct)
    {
        var sub = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(sub))
            throw new ServiceException(ServiceErrorCode.Unauthorized,
                "Brak claimu 'sub' w tokenie JWT.");

        var profile = await _userService.GetMeAsync(sub, ct);
        if (profile is null)
            throw new ServiceException(ServiceErrorCode.NotFound,
                "Użytkownik nie istnieje w bazie. Wywołaj POST /api/auth/me aby zainicjować konto.");

        return Ok(profile);
    }

    /// <summary>
    /// Ustawia preferowany język UI dla zalogowanego użytkownika.
    /// Dozwolone wartości: "pl", "en".
    /// </summary>
    [HttpPatch("me/language")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetLanguage([FromBody] SetPreferredLanguageRequest request, CancellationToken ct)
    {
        var sub = User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(sub))
            throw new ServiceException(ServiceErrorCode.Unauthorized,
                "Brak claimu 'sub' w tokenie JWT.");

        await _userService.SetPreferredLanguageAsync(sub, request.Language, ct);
        return NoContent();
    }

    /// <summary>
    /// Wydobywa klucz roli (np. "admin") z claims 'roles' zwracanych przez Kinde.
    /// Kinde zwraca tablicę obiektów JSON: [{"id":"...","key":"admin","name":"Admin"}].
    /// Obsługuje oba formaty: obiekt JSON {"key":"admin"} oraz plain string "admin".
    /// </summary>
    internal static string? ExtractKindeRoleKey(IEnumerable<Claim> rolesClaims)
    {
        foreach (var claim in rolesClaims)
        {
            var value = claim.Value;
            if (string.IsNullOrWhiteSpace(value)) continue;

            if (!value.TrimStart().StartsWith('{') && !value.TrimStart().StartsWith('['))
                return value;

            try
            {
                using var doc = JsonDocument.Parse(value);
                if (doc.RootElement.TryGetProperty("key", out var keyEl))
                    return keyEl.GetString();
            }
            catch (JsonException) { /* ignoruj niepoprawny JSON */ }
        }
        return null;
    }
}
