using BookingHub.Api.Dtos.Location;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania lokalizacjami (salami, obiektami) w organizacji.
/// </summary>
public interface ILocationService
{
    /// <summary>Pobiera stronicowaną listę lokalizacji w organizacji.</summary>
    Task<PagedResult<LocationSummaryResponse>> GetPagedAsync(Guid organizationId, LocationFilterParams filter, CancellationToken ct = default);

    /// <summary>Pobiera wszystkie aktywne lokalizacje organizacji (do selectów).</summary>
    Task<IReadOnlyList<LocationSummaryResponse>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły lokalizacji.</summary>
    Task<LocationDetailResponse> GetByIdAsync(Guid locationId, CancellationToken ct = default);

    /// <summary>Tworzy nową lokalizację.</summary>
    Task<LocationDetailResponse> CreateAsync(Guid organizationId, CreateLocationRequest request, CancellationToken ct = default);

    /// <summary>Aktualizuje dane lokalizacji.</summary>
    Task<LocationDetailResponse> UpdateAsync(Guid locationId, UpdateLocationRequest request, CancellationToken ct = default);

    /// <summary>Usuwa lokalizację (soft delete). Tylko gdy brak zaplanowanych zajęć.</summary>
    Task DeleteAsync(Guid locationId, CancellationToken ct = default);
}
