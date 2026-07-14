using BookingHub.Api.Dtos.Location;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ServiceException = BookingHub.Api.Services.Exceptions.ServiceException;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie lokalizacjami (salami, obiektami) w organizacji.
///
/// Trasa bazowa: /api/organizations/{organizationId}/locations
///
///   GET /              — lista stronicowana (Admin, Manager, Trainer)
///   GET /all           — pełna lista do selectów (Admin, Manager, Trainer)
///   GET /              — lista (dowolny aktywny członek)
///   GET /all           — pełna lista (dowolny aktywny członek)
///   GET /{locationId}  — szczegóły (dowolny aktywny członek)
///   POST /             — utwórz (Admin, Manager)
///   PUT  /{locationId} — edytuj (Admin, Manager)
///   DELETE /{locationId} — usuń (Admin)
/// </summary>
[Route("api/organizations/{organizationId:guid}/locations")]
[RequireOrgMembership]
public sealed class LocationsController : BookingHubControllerBase
{
    private readonly ILocationService _locations;

    public LocationsController(ILocationService locations)
    {
        _locations = locations;
    }

    /// <summary>
    /// Stronicowana lista lokalizacji z filtrowaniem.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LocationSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LocationSummaryResponse>>> GetPaged(
        Guid organizationId, [FromQuery] LocationFilterParams filter, CancellationToken ct)
    {
        var result = await _locations.GetPagedAsync(organizationId, filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Pełna lista aktywnych lokalizacji (do selectów, np. przy tworzeniu zajęć).
    /// </summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(IReadOnlyList<LocationSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LocationSummaryResponse>>> GetAll(
        Guid organizationId, CancellationToken ct)
    {
        var result = await _locations.GetByOrganizationAsync(organizationId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Szczegóły lokalizacji.
    /// </summary>
    [HttpGet("{locationId:guid}")]
    [ProducesResponseType(typeof(LocationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationDetailResponse>> GetById(
        Guid organizationId, Guid locationId, CancellationToken ct)
    {
        var location = await _locations.GetByIdAsync(locationId, ct);
        if (location.OrganizationId != organizationId)
            throw new Services.Exceptions.ServiceException(Services.Exceptions.ServiceErrorCode.NotFound,
                $"Lokalizacja {locationId} nie istnieje w tej organizacji.");
        return Ok(location);
    }

    /// <summary>
    /// Tworzy nową lokalizację. Admin lub Manager.
    /// </summary>
    [HttpPost]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(LocationDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LocationDetailResponse>> Create(
        Guid organizationId, [FromBody] CreateLocationRequest request, CancellationToken ct)
    {
        var created = await _locations.CreateAsync(organizationId, request, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, locationId = created.Id }, created);
    }

    /// <summary>
    /// Aktualizuje dane lokalizacji. Admin lub Manager.
    /// </summary>
    [HttpPut("{locationId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(LocationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LocationDetailResponse>> Update(
        Guid organizationId, Guid locationId,
        [FromBody] UpdateLocationRequest request, CancellationToken ct)
    {
        var updated = await _locations.UpdateAsync(locationId, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Usuwa lokalizację (soft delete). Tylko Admin. Wymaga braku zaplanowanych zajęć.
    /// </summary>
    [HttpDelete("{locationId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid organizationId, Guid locationId, CancellationToken ct)
    {
        await _locations.DeleteAsync(locationId, ct);
        return NoContent();
    }

    /// <summary>
    /// Podsumowanie zajętości sali w danym miesiącu — widok kalendarza miesięcznego.
    /// Dla każdego dnia zwraca liczbę zajęć, pokryte godziny i poziom zajętości (None/Partial/Full).
    /// Dostępne dla wszystkich aktywnych członków organizacji.
    /// </summary>
    [HttpGet("{locationId:guid}/schedule/month")]
    [ProducesResponseType(typeof(LocationMonthSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationMonthSummaryResponse>> GetMonthSchedule(
        Guid organizationId, Guid locationId,
        [FromQuery] int year, [FromQuery] int month,
        CancellationToken ct)
    {
        var location = await _locations.GetByIdAsync(locationId, ct);
        if (location.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Lokalizacja {locationId} nie istnieje w tej organizacji.");

        if (year is < 2020 or > 2100)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Rok musi być z zakresu 2020–2100.", nameof(year));

        if (month is < 1 or > 12)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Miesiąc musi być z zakresu 1–12.", nameof(month));

        var result = await _locations.GetMonthScheduleAsync(locationId, year, month, ct);
        return Ok(result);
    }

    /// <summary>
    /// Harmonogram sali dla konkretnego dnia — widok dzienny z blokami godzinowymi.
    /// Zwraca wszystkie zajęcia z liczebnościami uczestników (bez imion — prywatność).
    /// Dostępne dla wszystkich aktywnych członków organizacji.
    /// </summary>
    [HttpGet("{locationId:guid}/schedule/day")]
    [ProducesResponseType(typeof(LocationDayScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationDayScheduleResponse>> GetDaySchedule(
        Guid organizationId, Guid locationId,
        [FromQuery] string date,
        CancellationToken ct)
    {
        var location = await _locations.GetByIdAsync(locationId, ct);
        if (location.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Lokalizacja {locationId} nie istnieje w tej organizacji.");

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsedDate))
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Nieprawidłowy format daty. Oczekiwany: YYYY-MM-DD.", nameof(date));

        var result = await _locations.GetDayScheduleAsync(locationId, parsedDate, ct);
        return Ok(result);
    }
}
