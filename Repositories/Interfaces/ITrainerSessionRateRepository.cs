using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium historii stawek godzinowych trenerów (TrainerSessionRate).
/// </summary>
public interface ITrainerSessionRateRepository : IBaseRepository<TrainerSessionRate>
{
    /// <summary>
    /// Pobiera stronicowaną listę stawek z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<TrainerSessionRate>> GetPagedAsync(TrainerSessionRateFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie stawki danego trenera, posortowane malejąco po ValidFrom.
    /// </summary>
    Task<IReadOnlyList<TrainerSessionRate>> GetByTrainerAsync(Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera aktualnie obowiązującą stawkę trenera (ValidTo IS NULL).
    /// Zwraca null jeśli brak stawki.
    /// </summary>
    Task<TrainerSessionRate?> GetCurrentByTrainerAsync(Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stawkę trenera obowiązującą w konkretnej dacie.
    /// Uwzględnia historię zmian: ValidFrom &lt;= date &amp;&amp; (ValidTo IS NULL || ValidTo &gt;= date).
    /// </summary>
    Task<TrainerSessionRate?> GetRateOnDateAsync(Guid trainerMemberId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stawki dla wielu trenerów w podanej dacie — do rozliczeń zajęć indywidualnych.
    /// </summary>
    Task<Dictionary<Guid, TrainerSessionRate?>> GetRatesOnDateForTrainersAsync(IEnumerable<Guid> trainerMemberIds, DateOnly date, CancellationToken cancellationToken = default);
}
