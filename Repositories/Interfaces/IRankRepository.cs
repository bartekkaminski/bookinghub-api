using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium rang organizacyjnych.
/// </summary>
public interface IRankRepository : IBaseRepository<OrganizationRank>
{
    /// <summary>
    /// Pobiera wszystkie rangi w dyscyplinie posortowane alfabetycznie.
    /// </summary>
    Task<IReadOnlyList<OrganizationRank>> GetByDisciplineAsync(Guid disciplineId, CancellationToken ct = default);

    /// <summary>
    /// Pobiera stronicowaną listę aktywnych członków z daną rangą.
    /// </summary>
    Task<PagedResult<OrganizationMember>> GetPagedMembersAsync(Guid rankId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Zwraca liczbę aktywnych (niesoft-delete'owanych) członków posiadających tę rangę.
    /// </summary>
    Task<int> CountMembersAsync(Guid rankId, CancellationToken ct = default);

    /// <summary>
    /// Sprawdza, czy nazwa rangi jest już zajęta w ramach dyscypliny (case-sensitive).
    /// </summary>
    Task<bool> IsNameTakenAsync(Guid disciplineId, string name, Guid? excludeId = null, CancellationToken ct = default);
}
