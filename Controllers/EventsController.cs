using BookingHub.Api.Dtos.Event;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie zajęciami (Events) w organizacji.
///
/// Trasa bazowa: /api/organizations/{organizationId}/events
///
/// Odczyt:
///   GET /              — lista stronicowana (wszyscy członkowie)
///   GET /calendar      — widok kalendarza (wszyscy członkowie)
///   GET /my-calendar   — kalendarz zalogowanego uczestnika (Participant)
///   GET /{eventId}     — szczegóły (wszyscy członkowie)
///
/// Zarządzanie (Admin, Manager, Trainer):
///   POST /             — utwórz zajęcia (Admin, Manager, Trainer)
///   PUT  /{eventId}    — edytuj (Admin, Manager, Trainer)
///   POST /{eventId}/cancel   — odwołaj (Admin, Manager, Trainer)
///   POST /{eventId}/complete — zakończ (Admin, Manager, Trainer)
///   DELETE /{eventId}  — usuń (Admin, Manager)
///
/// Trenerzy:
///   POST   /{eventId}/trainers                 — przypisz trenera (Admin, Manager)
///   DELETE /{eventId}/trainers/{trainerId}     — usuń trenera (Admin, Manager)
/// </summary>
[Route("api/organizations/{organizationId:guid}/events")]
[RequireOrgMembership]
public sealed class EventsController : BookingHubControllerBase
{
    private readonly IEventService _events;

    public EventsController(IEventService events)
    {
        _events = events;
    }

    /// <summary>
    /// Stronicowana lista zajęć w organizacji.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventSummaryResponse>>> GetPaged(
        Guid organizationId, [FromQuery] EventFilterParams filter, CancellationToken ct)
    {
        var result = await _events.GetPagedAsync(organizationId, filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Widok kalendarza — zajęcia w przedziale dat dla organizacji.
    /// Optymalizowany pod renderowanie kalendarza (frontend).
    /// </summary>
    [HttpGet("calendar")]
    [ProducesResponseType(typeof(IReadOnlyList<EventCalendarResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<EventCalendarResponse>>> GetCalendar(
        Guid organizationId, [FromQuery] CalendarRequest request, CancellationToken ct)
    {
        if (request.From >= request.To)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "'from' musi być wcześniejsza niż 'to'.", "from");

        var result = await _events.GetCalendarAsync(organizationId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Kalendarz zalogowanego uczestnika — zajęcia, na które jest zapisany.
    /// Dostępny dla uczestnika (widzi swój harmonogram).
    /// </summary>
    [HttpGet("my-calendar")]
    [ProducesResponseType(typeof(IReadOnlyList<EventCalendarResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<EventCalendarResponse>>> GetMyCalendar(
        Guid organizationId, [FromQuery] CalendarRequest request, CancellationToken ct)
    {
        if (request.From >= request.To)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "'from' musi być wcześniejsza niż 'to'.", "from");

        var member = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember,
                "Nie jesteś członkiem tej organizacji.");

        var result = await _events.GetCalendarForMemberAsync(member.Id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Szczegóły zajęć: trenerzy, zapisy indywidualne i zespołowe.
    /// </summary>
    [HttpGet("{eventId:guid}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailResponse>> GetById(
        Guid organizationId, Guid eventId, CancellationToken ct)
    {
        var evt = await _events.GetByIdAsync(eventId, ct);
        if (evt.OrganizationId != organizationId)
            throw new Services.Exceptions.ServiceException(Services.Exceptions.ServiceErrorCode.NotFound,
                $"Zajęcia {eventId} nie istnieją w tej organizacji.");

        return Ok(evt);
    }

    /// <summary>
    /// Tworzy nowe zajęcia. Admin, Manager lub Trainer.
    /// Opcjonalnie przypisuje trenerów w tym samym żądaniu.
    /// </summary>
    [HttpPost]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailResponse>> Create(
        Guid organizationId, [FromBody] CreateEventRequest request, CancellationToken ct)
    {
        var created = await _events.CreateAsync(organizationId, request, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, eventId = created.Id }, created);
    }

    /// <summary>
    /// Aktualizuje dane zajęć. Dozwolone tylko gdy status = Scheduled.
    /// Admin, Manager lub Trainer.
    /// </summary>
    [HttpPut("{eventId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventDetailResponse>> Update(
        Guid organizationId, Guid eventId,
        [FromBody] UpdateEventRequest request, CancellationToken ct)
    {
        var updated = await _events.UpdateAsync(eventId, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Odwołuje zajęcia (status → Cancelled).
    /// Opcjonalnie wysyła automatyczne powiadomienie do uczestników.
    /// Admin, Manager lub Trainer.
    /// </summary>
    [HttpPost("{eventId:guid}/cancel")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventDetailResponse>> Cancel(
        Guid organizationId, Guid eventId,
        [FromBody] CancelEventRequest request, CancellationToken ct)
    {
        var updated = await _events.CancelAsync(eventId, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Kończy zajęcia (status → Completed).
    /// Zmienia wszystkie aktywne zapisy na Attended.
    /// Admin, Manager lub Trainer.
    /// </summary>
    [HttpPost("{eventId:guid}/complete")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventDetailResponse>> Complete(
        Guid organizationId, Guid eventId, CancellationToken ct)
    {
        var updated = await _events.CompleteAsync(eventId, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Usuwa zajęcia (soft delete). Tylko gdy status = Scheduled i brak zapisów.
    /// Admin lub Manager.
    /// </summary>
    [HttpDelete("{eventId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid organizationId, Guid eventId, CancellationToken ct)
    {
        await _events.DeleteAsync(eventId, ct);
        return NoContent();
    }

    // ── Trenerzy ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Przypisuje trenera do zajęć. Admin lub Manager.
    /// </summary>
    [HttpPost("{eventId:guid}/trainers")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventDetailResponse>> AssignTrainer(
        Guid organizationId, Guid eventId,
        [FromBody] AssignTrainerToEventRequest request, CancellationToken ct)
    {
        var updated = await _events.AssignTrainerAsync(eventId, request.OrganizationMemberId, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Usuwa przypisanie trenera z zajęć. Admin lub Manager.
    /// </summary>
    [HttpDelete("{eventId:guid}/trainers/{trainerId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailResponse>> RemoveTrainer(
        Guid organizationId, Guid eventId, Guid trainerId, CancellationToken ct)
    {
        var updated = await _events.RemoveTrainerAsync(eventId, trainerId, ct);
        return Ok(updated);
    }
}
