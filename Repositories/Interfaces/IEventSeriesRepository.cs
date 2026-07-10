using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium serii cyklicznych zajęć (EventSeries).
/// </summary>
public interface IEventSeriesRepository : IBaseRepository<EventSeries>
{
    /// <summary>
    /// Pobiera serię wraz z listą wszystkich powiązanych zajęć (Events).
    /// </summary>
    Task<EventSeries?> GetWithEventsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę serii z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<EventSeries>> GetPagedAsync(EventSeriesFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie aktywne serie w organizacji.
    /// </summary>
    Task<IReadOnlyList<EventSeries>> GetByOrganizationAsync(Guid organizationId, bool onlyActive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie serie powiązane z daną grupą jako domyślna.
    /// </summary>
    Task<IReadOnlyList<EventSeries>> GetByDefaultGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera serię z pełnymi danymi (Location, Group, Events count).
    /// </summary>
    Task<EventSeries?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
