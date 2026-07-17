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
///   GET /by-series-group/{seriesGroupId} — zajęcia w cyklu
///
/// Zarządzanie (Admin, Manager, Trainer):
///   POST /             — utwórz zajęcia jednorazowe
///   POST /recurring    — utwórz cykl zajęć (Admin, Manager)
///   PUT  /{eventId}    — edytuj
///   POST /{eventId}/cancel   — odwołaj
///   POST /{eventId}/complete — zakończ
///   DELETE /{eventId}  — usuń (Admin, Manager)
///   POST /by-series-group/{seriesGroupId}/cancel-future — odwołaj przyszłe w cyklu (Admin, Manager)
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
    /// Lista zajęć należących do cyklu (SeriesGroupId).
    /// </summary>
    [HttpGet("by-series-group/{seriesGroupId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<EventSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EventSummaryResponse>>> GetBySeriesGroup(
        Guid organizationId, Guid seriesGroupId, CancellationToken ct)
    {
        var result = await _events.GetBySeriesGroupAsync(organizationId, seriesGroupId, ct);
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
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Zajęcia {eventId} nie istnieją w tej organizacji.");

        return Ok(evt);
    }

    /// <summary>
    /// Tworzy nowe jednorazowe zajęcia. Admin, Manager lub Trainer.
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
        var member = await CurrentUser.GetMemberAsync(organizationId, ct);
        var created = await _events.CreateAsync(organizationId, request, member?.Id, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, eventId = created.Id }, created);
    }

    /// <summary>
    /// Tworzy cykl zajęć (wiele Event z tym samym SeriesGroupId) dla wybranych dni tygodnia
    /// w podanym zakresie dat. Admin lub Manager.
    /// </summary>
    [HttpPost("recurring")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(CreateRecurringEventsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateRecurringEventsResponse>> CreateRecurring(
        Guid organizationId, [FromBody] CreateRecurringEventsRequest request, CancellationToken ct)
    {
        var member = await CurrentUser.GetMemberAsync(organizationId, ct);
        var result = await _events.CreateRecurringAsync(organizationId, request, member?.Id, ct);
        return CreatedAtAction(nameof(GetBySeriesGroup),
            new { organizationId, seriesGroupId = result.SeriesGroupId }, result);
    }

    /// <summary>
    /// Odwołuje wszystkie przyszłe zaplanowane zajęcia w cyklu. Admin lub Manager.
    /// </summary>
    [HttpPost("by-series-group/{seriesGroupId:guid}/cancel-future")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(CancelFutureInSeriesGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CancelFutureInSeriesGroupResponse>> CancelFutureInSeriesGroup(
        Guid organizationId, Guid seriesGroupId,
        [FromBody] CancelFutureInSeriesGroupRequest request, CancellationToken ct)
    {
        var result = await _events.CancelFutureInSeriesGroupAsync(organizationId, seriesGroupId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Aktualizuje dane zajęć. Dozwolone tylko gdy status = Scheduled.
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
