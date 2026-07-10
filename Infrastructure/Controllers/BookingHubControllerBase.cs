using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Infrastructure.Controllers;

/// <summary>
/// Bazowy kontroler dla wszystkich endpointów BookingHub.
/// Zapewnia:
/// - [ApiController] + JSON validation responses
/// - [Authorize] — wymaga ważnego tokenu JWT
/// - Dostęp do ICurrentUserService przez właściwość CurrentUser
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class BookingHubControllerBase : ControllerBase
{
    private ICurrentUserService? _currentUser;

    /// <summary>
    /// Lazy-resolved CurrentUserService z DI.
    /// Korzystaj z tej właściwości zamiast wstrzykiwać ICurrentUserService ręcznie w każdym kontrolerze.
    /// </summary>
    protected ICurrentUserService CurrentUser =>
        _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

    /// <summary>
    /// Zwraca identyfikator zalogowanego użytkownika.
    /// Rzuca 401 jeśli użytkownik nie istnieje w bazie (nie wywołał POST /api/auth/me).
    /// </summary>
    protected Guid RequireUserId()
    {
        var id = CurrentUser.UserId;
        if (id is null)
            throw new Services.Exceptions.ServiceException(
                Services.Exceptions.ServiceErrorCode.Unauthorized,
                "Użytkownik nie jest zalogowany lub nie istnieje w bazie. Wywołaj POST /api/auth/me.");
        return id.Value;
    }

    /// <summary>
    /// Zwraca PersonId zalogowanego użytkownika.
    /// Rzuca 401 jeśli brak profilu osoby.
    /// </summary>
    protected Guid RequirePersonId()
    {
        var id = CurrentUser.PersonId;
        if (id is null)
            throw new Services.Exceptions.ServiceException(
                Services.Exceptions.ServiceErrorCode.Unauthorized,
                "Brak profilu osoby dla aktualnie zalogowanego użytkownika.");
        return id.Value;
    }
}
