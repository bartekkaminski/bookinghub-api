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

    /// <summary>
    /// Podsumowanie zajętości sali w danym miesiącu (widok kalendarza miesięcznego).
    /// Dla każdego dnia: liczba zajęć, pokryte godziny (unia interwałów) i poziom zajętości.
    /// </summary>
    Task<LocationMonthSummaryResponse> GetMonthScheduleAsync(Guid locationId, int year, int month, CancellationToken ct = default);

    /// <summary>
    /// Harmonogram sali dla konkretnego dnia (widok dzienny z blokami godzinowymi).
    /// Zwraca wszystkie zajęcia w lokalizacji wraz z liczebnościami uczestników (bez imion).
    /// </summary>
    Task<LocationDayScheduleResponse> GetDayScheduleAsync(Guid locationId, DateOnly date, CancellationToken ct = default);
}
