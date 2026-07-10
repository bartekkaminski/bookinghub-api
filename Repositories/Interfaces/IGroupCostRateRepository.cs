using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium historii stawek miesięcznych grup zajęciowych (GroupCostRate).
/// </summary>
public interface IGroupCostRateRepository : IBaseRepository<GroupCostRate>
{
    /// <summary>
    /// Pobiera stronicowaną listę stawek z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<GroupCostRate>> GetPagedAsync(GroupCostRateFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie stawki dla danej grupy, posortowane malejąco po ValidFrom.
    /// </summary>
    Task<IReadOnlyList<GroupCostRate>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera aktualnie obowiązującą stawkę dla grupy (ValidTo IS NULL).
    /// Zwraca null jeśli brak stawki.
    /// </summary>
    Task<GroupCostRate?> GetCurrentByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stawkę obowiązującą dla grupy w konkretnej dacie.
    /// Uwzględnia historię zmian: ValidFrom &lt;= date &amp;&amp; (ValidTo IS NULL || ValidTo &gt;= date).
    /// </summary>
    Task<GroupCostRate?> GetRateOnDateAsync(Guid groupId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stawki dla wielu grup w podanym miesiącu — do generowania rachunków zbiorczych.
    /// </summary>
    Task<Dictionary<Guid, GroupCostRate?>> GetRatesOnDateForGroupsAsync(IEnumerable<Guid> groupIds, DateOnly date, CancellationToken cancellationToken = default);
}
