using BookingHub.Api.Dtos.DeviceToken;
using BookingHub.Api.Dtos.User;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie kontami logowania (User + Kinde) oraz tokenami urządzeń FCM.
///
///   GET  /api/users/{id}                              — szczegóły konta
///   PATCH /api/users/{id}/active                      — zablokuj / odblokuj konto
///   DELETE /api/users/{id}                            — usuń konto (soft delete)
///   POST  /api/users/{id}/device-tokens               — zarejestruj token FCM
///   DELETE /api/users/{id}/device-tokens/{token}      — wyrejestruj token FCM
/// </summary>
[Route("api/users")]
[Authorize(Policy = ApiPolicies.AuthenticatedUser)]
public sealed class UsersController : BookingHubControllerBase
{
    private readonly IUserService _users;
    private readonly IUserDeviceTokenRepository _deviceTokens;

    public UsersController(IUserService users, IUserDeviceTokenRepository deviceTokens)
    {
        _users = users;
        _deviceTokens = deviceTokens;
    }

    /// <summary>
    /// Szczegóły konta logowania.
    /// Użytkownik może pobierać tylko własne konto.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var myUserId = RequireUserId();
        if (myUserId != id)
            throw new Services.Exceptions.ServiceException(
                Services.Exceptions.ServiceErrorCode.Forbidden,
                "Możesz pobierać tylko własne dane konta.");

        var user = await _users.GetByIdAsync(id, ct);
        return Ok(user);
    }

    /// <summary>
    /// Zmienia aktywność konta (globalny toggle).
    /// Operacja synchronizowana z Kinde (suspend / unsuspend).
    /// Dostęp tylko z poziomu Admina organizacji — wywołują admin kontroler członkostw,
    /// lub bezpośrednio przez endpoint np. użytkownik dezaktywuje własne konto.
    /// </summary>
    [HttpPatch("{id:guid}/active")]
    [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailResponse>> SetActive(
        Guid id, [FromBody] SetUserActiveRequest request, CancellationToken ct)
    {
        var myUserId = RequireUserId();
        if (myUserId != id)
            throw new Services.Exceptions.ServiceException(
                Services.Exceptions.ServiceErrorCode.Forbidden,
                "Możesz zmieniać aktywność tylko własnego konta.");

        var updated = await _users.SetActiveAsync(id, request.IsActive, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Usuwa konto (soft delete). Nie usuwa profilu Person ani członkostw.
    /// Użytkownik może usunąć tylko własne konto.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var myUserId = RequireUserId();
        if (myUserId != id)
            throw new Services.Exceptions.ServiceException(
                Services.Exceptions.ServiceErrorCode.Forbidden,
                "Możesz usunąć tylko własne konto.");

        await _users.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Rejestruje token FCM dla urządzenia użytkownika.
    /// Idempotentne — ponowna rejestracja tego samego tokenu jest ignorowana.
    /// </summary>
    [HttpPost("{id:guid}/device-tokens")]
    [ProducesResponseType(typeof(DeviceTokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceTokenResponse>> RegisterDeviceToken(
        Guid id, [FromBody] RegisterDeviceTokenRequest request, CancellationToken ct)
    {
        var myUserId = RequireUserId();
        if (myUserId != id)
            throw new Services.Exceptions.ServiceException(
                Services.Exceptions.ServiceErrorCode.Forbidden,
                "Możesz rejestrować tokeny tylko dla własnego konta.");

        // Idempotency — jeśli token już istnieje, zwróć conflict-free success
        if (await _deviceTokens.ExistsAsync(id, request.Token, ct))
        {
            var existing = (await _deviceTokens.GetByUserIdAsync(id, ct))
                .First(t => t.Token == request.Token);
            return CreatedAtAction(nameof(RegisterDeviceToken), new { id },
                new DeviceTokenResponse { Id = existing.Id, Platform = existing.Platform, CreatedAt = existing.CreatedAt });
        }

        var token = new Models.UserDeviceToken
        {
            UserId   = id,
            Token    = request.Token,
            Platform = request.Platform,
        };

        var saved = await _deviceTokens.AddAsync(token, ct);
        return CreatedAtAction(nameof(RegisterDeviceToken), new { id },
            new DeviceTokenResponse { Id = saved.Id, Platform = saved.Platform, CreatedAt = saved.CreatedAt });
    }

    /// <summary>
    /// Wyrejestrowuje token FCM urządzenia użytkownika (np. przy wylogowaniu).
    /// </summary>
    [HttpDelete("{id:guid}/device-tokens/{token}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDeviceToken(Guid id, string token, CancellationToken ct)
    {
        var myUserId = RequireUserId();
        if (myUserId != id)
            throw new Services.Exceptions.ServiceException(
                Services.Exceptions.ServiceErrorCode.Forbidden,
                "Możesz usuwać tokeny tylko dla własnego konta.");

        var deleted = await _deviceTokens.DeleteAsync(id, token, ct);
        if (!deleted)
            throw new Services.Exceptions.ServiceException(
                Services.Exceptions.ServiceErrorCode.NotFound,
                "Token urządzenia nie istnieje.");

        return NoContent();
    }
}
