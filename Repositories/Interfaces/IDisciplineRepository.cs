using BookingHub.Api.Models;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium dyscyplin organizacyjnych.
/// </summary>
public interface IDisciplineRepository : IBaseRepository<Discipline>
{
    /// <summary>
    /// Pobiera wszystkie dyscypliny w organizacji posortowane alfabetycznie.
    /// </summary>
    Task<IReadOnlyList<Discipline>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Zwraca liczbę rang zdefiniowanych w tej dyscyplinie.
    /// </summary>
    Task<int> CountRanksAsync(Guid disciplineId, CancellationToken ct = default);

    /// <summary>
    /// Sprawdza, czy nazwa dyscypliny jest już zajęta w organizacji (case-sensitive).
    /// </summary>
    Task<bool> IsNameTakenAsync(Guid organizationId, string name, Guid? excludeId = null, CancellationToken ct = default);
}
