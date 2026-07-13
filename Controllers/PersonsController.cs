using BookingHub.Api.Dtos.Person;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie profilami osób (Person).
/// Person istnieje niezależnie od konta logowania — np. dziecko może nie mieć konta.
///
///   GET    /api/persons/me                        — własny profil zalogowanego
///   GET    /api/persons/{personId}                — profil osoby (własny lub admin)
///   PUT    /api/persons/{personId}                — edycja profilu (własny lub admin)
///   DELETE /api/persons/{personId}                — usunięcie (admin)
///
///   GET    /api/persons/{personId}/children       — lista dzieci (własna lub admin)
///   POST   /api/persons/{personId}/children       — dodaj relację rodzic–dziecko (własna lub admin)
///   DELETE /api/persons/{personId}/children/{childPersonId} — usuń relację (własna lub admin)
/// </summary>
[Route("api/persons")]
public sealed class PersonsController : BookingHubControllerBase
{
    private readonly IPersonService _persons;
    private readonly IOrganizationMemberRepository _memberRepo;

    public PersonsController(IPersonService persons, IOrganizationMemberRepository memberRepo)
    {
        _persons   = persons;
        _memberRepo = memberRepo;
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
    /// Dostęp: własny profil lub admin w organizacji, do której osoba należy.
    /// </summary>
    [HttpGet("{personId:guid}")]
    [ProducesResponseType(typeof(PersonDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonDetailResponse>> GetById(Guid personId, CancellationToken ct)
    {
        await RequireAccessToPersonAsync(personId, ct);
        var person = await _persons.GetByIdAsync(personId, ct);

        // Kod profilu jest widoczny wyłącznie dla właściciela konta —
        // admin oglądający cudzy profil nie powinien go widzieć (zapobiega social engineering)
        if (CurrentUser.PersonId != personId)
            person.ProfileCode = null;

        return Ok(person);
    }

    /// <summary>
    /// Aktualizuje dane profilu osoby (imię, nazwisko, data urodzenia, zdjęcie).
    /// Dostęp: własny profil lub admin.
    /// </summary>
    [HttpPut("{personId:guid}")]
    [ProducesResponseType(typeof(PersonDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonDetailResponse>> Update(
        Guid personId, [FromBody] UpdatePersonRequest request, CancellationToken ct)
    {
        await RequireAccessToPersonAsync(personId, ct);
        var updated = await _persons.UpdateAsync(personId, request, ct);
        return Ok(updated);
    }

    // ── Relacje rodzic–dziecko ─────────────────────────────────────────────────

    /// <summary>
    /// Pobiera listę dzieci danej osoby.
    /// Dostęp: własny profil lub admin.
    /// </summary>
    [HttpGet("{personId:guid}/children")]
    [ProducesResponseType(typeof(IReadOnlyList<PersonSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PersonSummaryResponse>>> GetChildren(
        Guid personId, CancellationToken ct)
    {
        await RequireAccessToPersonAsync(personId, ct);
        var children = await _persons.GetChildrenAsync(personId, ct);
        return Ok(children);
    }

    /// <summary>
    /// Tworzy relację rodzic–dziecko.
    /// Dostęp: własny profil (jako rodzic) lub admin.
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
        await RequireAccessToPersonAsync(personId, ct);
        await _persons.AddParentChildRelationAsync(personId, request.ChildPersonId, ct);
        return NoContent();
    }

    /// <summary>
    /// Usuwa relację rodzic–dziecko.
    /// Dostęp: własny profil (jako rodzic) lub admin.
    /// </summary>
    [HttpDelete("{personId:guid}/children/{childPersonId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveChild(
        Guid personId, Guid childPersonId, CancellationToken ct)
    {
        await RequireAccessToPersonAsync(personId, ct);
        await _persons.RemoveParentChildRelationAsync(personId, childPersonId, ct);
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Weryfikuje czy zalogowany użytkownik ma prawo do operacji na danej osobie.
    /// Dozwolone: własny profil LUB admin w co najmniej jednej wspólnej organizacji.
    /// </summary>
    private async Task RequireAccessToPersonAsync(Guid targetPersonId, CancellationToken ct)
    {
        var myPersonId = CurrentUser.PersonId;

        // Własny profil — zawsze OK
        if (myPersonId == targetPersonId)
            return;

        // Sprawdź czy current user jest adminem w jakiejkolwiek organizacji,
        // do której należy też docelowa osoba
        var targetMemberships = await _memberRepo.GetByPersonIdAsync(targetPersonId, ct);
        foreach (var membership in targetMemberships)
        {
            if (await CurrentUser.IsAdminAsync(membership.OrganizationId, ct))
                return;
        }

        throw new ServiceException(ServiceErrorCode.Forbidden,
            "Brak dostępu. Wymagany własny profil lub rola Administratora w organizacji tej osoby.");
    }
}
