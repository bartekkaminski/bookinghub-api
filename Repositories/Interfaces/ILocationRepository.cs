using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium lokalizacji zajęć.
/// </summary>
public interface ILocationRepository : IBaseRepository<Location>
{
    /// <summary>
    /// Pobiera stronicowaną listę lokalizacji z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<Location>> GetPagedAsync(LocationFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie aktywne lokalizacje w organizacji.
    /// </summary>
    Task<IReadOnlyList<Location>> GetByOrganizationAsync(Guid organizationId, bool onlyActive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy nazwa lokalizacji jest już zajęta w organizacji.
    /// </summary>
    Task<bool> IsNameTakenInOrgAsync(Guid organizationId, string name, Guid? excludeLocationId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Alias z konwencją usług (IsNameTakenAsync).
    /// </summary>
    Task<bool> IsNameTakenAsync(Guid organizationId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy lokalizacja ma zaplanowane zajęcia w przyszłości.
    /// </summary>
    Task<bool> HasUpcomingEventsAsync(Guid locationId, CancellationToken cancellationToken = default);
}
