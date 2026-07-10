using BookingHub.Api.Dtos.Availability;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie dostępnością uczestników i trenerów.
///
/// Trasa bazowa: /api/organizations/{organizationId}/members/{memberId}/availability
///
///   GET /                — wszystkie sloty danego członka (Admin, Manager, Trainer lub właściciel)
///   GET /check           — sprawdzenie dostępności wielu osób (Admin, Manager, Trainer)
///   POST /               — dodaj slot (Admin, Manager lub właściciel)
///   PUT  /{slotId}       — edytuj slot (Admin, Manager lub właściciel)
///   DELETE /{slotId}     — usuń slot (Admin, Manager lub właściciel)
/// </summary>
[Route("api/organizations/{organizationId:guid}/members/{memberId:guid}/availability")]
[RequireOrgMembership]
public sealed class AvailabilityController : BookingHubControllerBase
{
    private readonly IAvailabilityService _availability;
    private readonly IOrganizationMemberRepository _members;

    public AvailabilityController(IAvailabilityService availability, IOrganizationMemberRepository members)
    {
        _availability = availability;
        _members      = members;
    }

    /// <summary>
    /// Pobiera wszystkie sloty dostępności danego członka.
    /// Admin/Manager/Trainer widzi każdego; uczestnik tylko siebie.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AvailabilitySlotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AvailabilitySlotResponse>>> GetByMember(
        Guid organizationId, Guid memberId, CancellationToken ct)
    {
        await EnsureCanAccessMemberDataAsync(organizationId, memberId, ct);

        var result = await _availability.GetByMemberAsync(memberId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Dodaje nowy slot dostępności.
    /// Admin/Manager może dodać dla każdego; uczestnik tylko dla siebie.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AvailabilitySlotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AvailabilitySlotResponse>> AddSlot(
        Guid organizationId, Guid memberId,
        [FromBody] AddAvailabilitySlotRequest request, CancellationToken ct)
    {
        await EnsureCanAccessMemberDataAsync(organizationId, memberId, ct);

        var created = await _availability.AddSlotAsync(memberId, request, ct);
        return CreatedAtAction(nameof(GetByMember),
            new { organizationId, memberId }, created);
    }

    /// <summary>
    /// Aktualizuje slot dostępności.
    /// Admin/Manager może edytować dla każdego; uczestnik tylko swoje.
    /// </summary>
    [HttpPut("{slotId:guid}")]
    [ProducesResponseType(typeof(AvailabilitySlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilitySlotResponse>> UpdateSlot(
        Guid organizationId, Guid memberId, Guid slotId,
        [FromBody] UpdateAvailabilitySlotRequest request, CancellationToken ct)
    {
        await EnsureCanAccessMemberDataAsync(organizationId, memberId, ct);

        var updated = await _availability.UpdateSlotAsync(slotId, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Usuwa slot dostępności.
    /// Admin/Manager może usunąć dla każdego; uczestnik tylko swoje.
    /// </summary>
    [HttpDelete("{slotId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSlot(
        Guid organizationId, Guid memberId, Guid slotId, CancellationToken ct)
    {
        await EnsureCanAccessMemberDataAsync(organizationId, memberId, ct);

        await _availability.DeleteSlotAsync(slotId, ct);
        return NoContent();
    }

    // ── Sprawdzenie dostępności wielu osób ────────────────────────────────────

    /// <summary>
    /// Sprawdza dostępność wielu członków w podanym przedziale czasu.
    /// Używane przy planowaniu zajęć do weryfikacji dostępności trenerów.
    /// Admin, Manager lub Trainer.
    /// </summary>
    [HttpGet("/api/organizations/{organizationId:guid}/availability/check")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(AvailabilityCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvailabilityCheckResponse>> CheckAvailability(
        Guid organizationId,
        [FromQuery] IReadOnlyList<Guid> memberIds,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        if (from >= to)
            throw new Services.Exceptions.ServiceException(Services.Exceptions.ServiceErrorCode.ValidationError,
                "'from' musi być wcześniejsza niż 'to'.", "from");

        if (!memberIds.Any())
            throw new Services.Exceptions.ServiceException(Services.Exceptions.ServiceErrorCode.ValidationError,
                "Podaj co najmniej jednego memberId.", "memberIds");

        var result = await _availability.CheckAvailabilityAsync(memberIds, from, to, ct);
        return Ok(result);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task EnsureCanAccessMemberDataAsync(Guid organizationId, Guid memberId, CancellationToken ct)
    {
        // Weryfikacja że memberId należy do organizacji z trasy (ochrona przed IDOR).
        var member = await _members.GetByIdAsync(memberId, ct);
        if (member is null || member.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Członek {memberId} nie istnieje w tej organizacji.");

        var isPrivileged = await CurrentUser.IsAdminOrManagerAsync(organizationId, ct)
                        || await CurrentUser.IsTrainerAsync(organizationId, ct);
        if (isPrivileged) return;

        var myMember = await CurrentUser.GetMemberAsync(organizationId, ct);
        if (myMember?.Id != memberId)
            throw new ServiceException(ServiceErrorCode.Forbidden,
                "Możesz zarządzać tylko własną dostępnością.");
    }
}
