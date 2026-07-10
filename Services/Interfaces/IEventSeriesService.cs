using BookingHub.Api.Dtos.EventSeries;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania seriami cyklicznych zajęć.
/// </summary>
public interface IEventSeriesService
{
    /// <summary>Pobiera stronicowaną listę serii w organizacji.</summary>
    Task<PagedResult<EventSeriesSummaryResponse>> GetPagedAsync(Guid organizationId, EventSeriesFilterParams filter, CancellationToken ct = default);

    /// <summary>Pobiera wszystkie aktywne serie organizacji (do selectów).</summary>
    Task<IReadOnlyList<EventSeriesSummaryResponse>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły serii.</summary>
    Task<EventSeriesDetailResponse> GetByIdAsync(Guid seriesId, CancellationToken ct = default);

    /// <summary>Tworzy nową serię cykliczną.</summary>
    Task<EventSeriesDetailResponse> CreateAsync(Guid organizationId, CreateEventSeriesRequest request, CancellationToken ct = default);

    /// <summary>Aktualizuje dane serii.</summary>
    Task<EventSeriesDetailResponse> UpdateAsync(Guid seriesId, UpdateEventSeriesRequest request, CancellationToken ct = default);

    /// <summary>Usuwa serię (soft delete).</summary>
    Task DeleteAsync(Guid seriesId, CancellationToken ct = default);

    /// <summary>
    /// Generuje zajęcia dla serii na podstawie reguły RRULE w podanym zakresie dat.
    /// Pomija daty, dla których zajęcia o tym samym StartTime w tej serii już istnieją.
    /// </summary>
    Task<GenerateEventsResponse> GenerateEventsAsync(Guid organizationId, Guid seriesId, GenerateEventsRequest request, CancellationToken ct = default);
}
