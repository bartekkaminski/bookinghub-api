using BookingHub.Api.Dtos.Location;
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

    public LocationService(
        ILocationRepository locations,
        IOrganizationRepository organizations)
    {
        _locations     = locations;
        _organizations = organizations;
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
}
