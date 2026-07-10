using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium organizacji (placówek — szkół tańca, klubów itp.).
/// </summary>
public interface IOrganizationRepository : IBaseRepository<Organization>
{
    /// <summary>
    /// Pobiera organizację z pełną listą członków (OrganizationMember z rolami i profilem osoby).
    /// </summary>
    Task<Organization?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę organizacji z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<Organization>> GetPagedAsync(OrganizationFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy nazwa organizacji jest już zajęta.
    /// </summary>
    Task<bool> IsNameTakenAsync(string name, Guid? excludeOrganizationId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca liczbę organizacji utworzonych przez daną osobę (CreatedByPersonId).
    /// </summary>
    Task<int> CountCreatedByPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie organizacje, do których należy dana osoba.
    /// </summary>
    Task<IReadOnlyList<Organization>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Alias dla GetWithMembersAsync — pobiera organizację z pełnymi danymi.
    /// </summary>
    Task<Organization?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
