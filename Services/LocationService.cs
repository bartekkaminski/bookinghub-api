using BookingHub.Api.Dtos.Location;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania lokalizacjami (salami, obiektami) w organizacji.
/// </summary>
public sealed class LocationService : ILocationService
{
    private readonly ILocationRepository _locations;
    private readonly IOrganizationRepository _organizations;
    private readonly IEventRepository _events;

    public LocationService(
        ILocationRepository locations,
        IOrganizationRepository organizations,
        IEventRepository events)
    {
        _locations     = locations;
        _organizations = organizations;
        _events        = events;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<LocationSummaryResponse>> GetPagedAsync(Guid organizationId, LocationFilterParams filter, CancellationToken ct = default)
    {
        filter.OrganizationId = organizationId;
        var paged = await _locations.GetPagedAsync(filter, ct);
        return paged.Map(l => l.ToSummary());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LocationSummaryResponse>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var locations = await _locations.GetByOrganizationAsync(organizationId, onlyActive: true, ct);
        return locations.Select(l => l.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<LocationDetailResponse> GetByIdAsync(Guid locationId, CancellationToken ct = default)
    {
        var location = await _locations.GetByIdAsync(locationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Lokalizacja {locationId} nie istnieje.");
        return location.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<LocationDetailResponse> CreateAsync(Guid organizationId, CreateLocationRequest request, CancellationToken ct = default)
    {
        var orgExists = await _organizations.ExistsAsync(organizationId, ct);
        if (!orgExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {organizationId} nie istnieje.");

        if (await _locations.IsNameTakenInOrgAsync(organizationId, request.Name, null, ct))
            throw new ServiceException(ServiceErrorCode.LocationNameTaken,
                $"Nazwa lokalizacji '{request.Name}' jest już zajęta w tej organizacji.", nameof(request.Name));

        var entity  = request.ToEntity(organizationId);
        var created = await _locations.AddAsync(entity, ct);
        return created.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<LocationDetailResponse> UpdateAsync(Guid locationId, UpdateLocationRequest request, CancellationToken ct = default)
    {
        var location = await _locations.GetByIdAsync(locationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Lokalizacja {locationId} nie istnieje.");

        if (!string.Equals(location.Name, request.Name, StringComparison.OrdinalIgnoreCase) &&
            await _locations.IsNameTakenInOrgAsync(location.OrganizationId, request.Name, excludeLocationId: locationId, ct))
            throw new ServiceException(ServiceErrorCode.LocationNameTaken,
                $"Nazwa lokalizacji '{request.Name}' jest już zajęta.", nameof(request.Name));

        location.ApplyUpdate(request);
        await _locations.UpdateAsync(location, ct);
        return location.ToDetail();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid locationId, CancellationToken ct = default)
    {
        var location = await _locations.GetByIdAsync(locationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Lokalizacja {locationId} nie istnieje.");

        var hasUpcoming = await _locations.HasUpcomingEventsAsync(locationId, ct);
        if (hasUpcoming)
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Nie można usunąć lokalizacji z zaplanowanymi zajęciami.");

        await _locations.DeleteAsync(locationId, ct);
    }

    /// <inheritdoc/>
    public async Task<LocationMonthSummaryResponse> GetMonthScheduleAsync(
        Guid locationId, int year, int month, CancellationToken ct = default)
    {
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = from.AddMonths(1);

        var events = await _events.GetByLocationAndRangeAsync(locationId, from, to, ct);

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var days        = new List<LocationDaySummary>(daysInMonth);

        for (var day = 1; day <= daysInMonth; day++)
        {
            var dayStart = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
            var dayEnd   = dayStart.AddDays(1);

            // Wszystkie zajęcia (łącznie z odwołanymi) dla licznika
            var allDayEvents = events
                .Where(e => e.StartTime < dayEnd && e.EndTime > dayStart)
                .ToList();

            // Tylko aktywne zajęcia do obliczenia pokrycia godzinowego
            var activeEvents = allDayEvents
                .Where(e => e.Status != EventStatus.Cancelled)
                .ToList();

            var coveredHours = CalculateCoveredHours(activeEvents, dayStart, dayEnd);

            days.Add(new LocationDaySummary
            {
                Date         = new DateOnly(year, month, day),
                EventCount   = allDayEvents.Count,
                CoveredHours = coveredHours,
                Occupancy    = coveredHours == 0   ? LocationOccupancy.None
                             : coveredHours < 8    ? LocationOccupancy.Partial
                             : LocationOccupancy.Full,
            });
        }

        return new LocationMonthSummaryResponse { Year = year, Month = month, Days = days };
    }

    /// <inheritdoc/>
    public async Task<LocationDayScheduleResponse> GetDayScheduleAsync(
        Guid locationId, DateOnly date, CancellationToken ct = default)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd   = dayStart.AddDays(1);

        var events  = await _events.GetByLocationAndRangeAsync(locationId, dayStart, dayEnd, ct);
        var mapped  = events.Select(e => e.ToLocationDayEvent()).ToList();

        return new LocationDayScheduleResponse { Date = date, Events = mapped };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Oblicza łączną liczbę godzin pokrytych przez listę zajęć w danym dniu,
    /// używając algorytmu unii interwałów (bez podwójnego liczenia zakładek).
    /// </summary>
    private static double CalculateCoveredHours(
        IReadOnlyList<Event> events, DateTime dayStart, DateTime dayEnd)
    {
        if (events.Count == 0) return 0;

        var intervals = events
            .Select(e => (
                start: e.StartTime < dayStart ? dayStart : e.StartTime,
                end:   e.EndTime   > dayEnd   ? dayEnd   : e.EndTime))
            .OrderBy(i => i.start)
            .ToList();

        var covered = TimeSpan.Zero;
        var cursor  = dayStart;

        foreach (var (start, end) in intervals)
        {
            if (start > cursor) cursor = start;
            if (end   > cursor) { covered += end - cursor; cursor = end; }
        }

        return covered.TotalHours;
    }
}
