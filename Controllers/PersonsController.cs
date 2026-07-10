using BookingHub.Api.Dtos.Person;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie profilami osób (Person).
/// Person istnieje niezależnie od konta logowania — np. dziecko może nie mieć konta.
///
///   GET    /api/persons/me                        — własny profil zalogowanego
///   GET    /api/persons/{personId}                — profil osoby (tylko własny lub admin)
///   PUT    /api/persons/{personId}                — edycja profilu (tylko własny lub admin)
///   DELETE /api/persons/{personId}                — usunięcie (admin)
///
///   POST   /api/persons/{personId}/children       — dodaj relację rodzic–dziecko
///   DELETE /api/persons/{personId}/children/{childPersonId} — usuń relację
/// </summary>
[Route("api/persons")]
public sealed class PersonsController : BookingHubControllerBase
{
    private readonly IPersonService _persons;

    public PersonsController(IPersonService persons)
    {
        _persons = persons;
    }

    /// <summary>
    /// Profil zalogowanego użytkownika wraz z jego członkostwami, dziećmi i rodzicami.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(PersonDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonDetailResponse>> GetMe(CancellationToken ct)
    {
        var userId = RequireUserId();
        var person = await _persons.GetByUserIdAsync(userId, ct);
        if (person is null)
            throw new ServiceException(ServiceErrorCode.NotFound,
                "Profil osoby nie istnieje. Wywołaj POST /api/auth/me aby zainicjować konto.");

        return Ok(person);
    }

    /// <summary>
    /// Profil osoby po Id.
    /// Zalogowany użytkownik może pobrać tylko swój profil; wymagane wywołanie z poziomu admina org.
    /// </summary>
    [HttpGet("{personId:guid}")]
    [ProducesResponseType(typeof(PersonDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonDetailResponse>> GetById(Guid personId, CancellationToken ct)
    {
        var myPersonId = CurrentUser.PersonId;
        if (myPersonId != personId)
            throw new ServiceException(ServiceErrorCode.Forbidden,
                "Możesz pobierać tylko własny profil.");

        var person = await _persons.GetByIdAsync(personId, ct);
        return Ok(person);
    }

    /// <summary>
    /// Aktualizuje dane profilu osoby (imię, nazwisko, data urodzenia, zdjęcie).
    /// Zalogowany użytkownik może edytować tylko swój profil.
    /// </summary>
    [HttpPut("{personId:guid}")]
    [ProducesResponseType(typeof(PersonDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonDetailResponse>> Update(
        Guid personId, [FromBody] UpdatePersonRequest request, CancellationToken ct)
    {
        var myPersonId = CurrentUser.PersonId;
        if (myPersonId != personId)
            throw new ServiceException(ServiceErrorCode.Forbidden,
                "Możesz edytować tylko własny profil.");

        var updated = await _persons.UpdateAsync(personId, request, ct);
        return Ok(updated);
    }

    // ── Relacje rodzic–dziecko ─────────────────────────────────────────────────

    /// <summary>
    /// Pobiera listę dzieci danej osoby.
    /// </summary>
    [HttpGet("{personId:guid}/children")]
    [ProducesResponseType(typeof(IReadOnlyList<PersonSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PersonSummaryResponse>>> GetChildren(
        Guid personId, CancellationToken ct)
    {
        var myPersonId = CurrentUser.PersonId;
        if (myPersonId != personId)
            throw new ServiceException(ServiceErrorCode.Forbidden,
                "Możesz przeglądać tylko własne relacje.");

        var children = await _persons.GetChildrenAsync(personId, ct);
        return Ok(children);
    }

    /// <summary>
    /// Tworzy relację rodzic–dziecko.
    /// Zalogowany użytkownik może dodać powiązanie tylko do swojego profilu (jako rodzic).
    /// </summary>
    [HttpPost("{personId:guid}/children")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddChild(
        Guid personId, [FromBody] AddParentChildRequest request, CancellationToken ct)
    {
        var myPersonId = CurrentUser.PersonId;
        if (myPersonId != personId)
            throw new ServiceException(ServiceErrorCode.Forbidden,
                "Możesz zarządzać tylko relacjami własnego profilu.");

        await _persons.AddParentChildRelationAsync(personId, request.ChildPersonId, ct);
        return NoContent();
    }

    /// <summary>
    /// Usuwa relację rodzic–dziecko.
    /// </summary>
    [HttpDelete("{personId:guid}/children/{childPersonId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveChild(
        Guid personId, Guid childPersonId, CancellationToken ct)
    {
        var myPersonId = CurrentUser.PersonId;
        if (myPersonId != personId)
            throw new ServiceException(ServiceErrorCode.Forbidden,
                "Możesz zarządzać tylko relacjami własnego profilu.");

        await _persons.RemoveParentChildRelationAsync(personId, childPersonId, ct);
        return NoContent();
    }
}
