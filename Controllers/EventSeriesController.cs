using BookingHub.Api.Dtos.EventSeries;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie seriami cyklicznych zajęć (szablony do generowania powtarzających się wydarzeń).
///
/// Trasa bazowa: /api/organizations/{organizationId}/event-series
///
///   GET /               — lista stronicowana (Admin, Manager, Trainer)
///   GET /all            — pełna lista do selectów (Admin, Manager, Trainer)
///   GET /{seriesId}     — szczegóły (Admin, Manager, Trainer)
///   POST /              — utwórz serię (Admin, Manager)
///   PUT  /{seriesId}    — edytuj (Admin, Manager)
///   DELETE /{seriesId}  — usuń (Admin)
/// </summary>
[Route("api/organizations/{organizationId:guid}/event-series")]
[RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
public sealed class EventSeriesController : BookingHubControllerBase
{
    private readonly IEventSeriesService _series;

    public EventSeriesController(IEventSeriesService series)
    {
        _series = series;
    }

    /// <summary>
    /// Stronicowana lista serii cyklicznych z filtrowaniem.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventSeriesSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventSeriesSummaryResponse>>> GetPaged(
        Guid organizationId, [FromQuery] EventSeriesFilterParams filter, CancellationToken ct)
    {
        var result = await _series.GetPagedAsync(organizationId, filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Pełna lista aktywnych serii (do selectów przy tworzeniu zajęć).
    /// </summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(IReadOnlyList<EventSeriesSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EventSeriesSummaryResponse>>> GetAll(
        Guid organizationId, CancellationToken ct)
    {
        var result = await _series.GetByOrganizationAsync(organizationId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Szczegóły serii cyklicznej.
    /// </summary>
    [HttpGet("{seriesId:guid}")]
    [ProducesResponseType(typeof(EventSeriesDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventSeriesDetailResponse>> GetById(
        Guid organizationId, Guid seriesId, CancellationToken ct)
    {
        var series = await _series.GetByIdAsync(seriesId, ct);
        if (series.OrganizationId != organizationId)
            throw new Services.Exceptions.ServiceException(Services.Exceptions.ServiceErrorCode.NotFound,
                $"Seria {seriesId} nie istnieje w tej organizacji.");
        return Ok(series);
    }

    /// <summary>
    /// Tworzy nową serię cykliczną. Admin lub Manager.
    /// </summary>
    [HttpPost]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(EventSeriesDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventSeriesDetailResponse>> Create(
        Guid organizationId, [FromBody] CreateEventSeriesRequest request, CancellationToken ct)
    {
        var created = await _series.CreateAsync(organizationId, request, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, seriesId = created.Id }, created);
    }

    /// <summary>
    /// Aktualizuje dane serii. Admin lub Manager.
    /// </summary>
    [HttpPut("{seriesId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(EventSeriesDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventSeriesDetailResponse>> Update(
        Guid organizationId, Guid seriesId,
        [FromBody] UpdateEventSeriesRequest request, CancellationToken ct)
    {
        var updated = await _series.UpdateAsync(seriesId, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Usuwa serię (soft delete). Tylko Admin.
    /// </summary>
    [HttpDelete("{seriesId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid organizationId, Guid seriesId, CancellationToken ct)
    {
        await _series.DeleteAsync(seriesId, ct);
        return NoContent();
    }
}
