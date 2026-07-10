using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium zespołów (par, formacji, drużyn).
/// </summary>
public interface ITeamRepository : IBaseRepository<Team>
{
    /// <summary>
    /// Pobiera zespół z listą zawodników (TeamMember → OrganizationMember → Person).
    /// </summary>
    Task<Team?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera zespół z pełnymi danymi — Members, Groups, Trainers.
    /// </summary>
    Task<Team?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę zespołów z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<Team>> GetPagedAsync(TeamFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie aktywne zespoły w organizacji, opcjonalnie posortowane po priorytecie.
    /// </summary>
    Task<IReadOnlyList<Team>> GetByOrganizationAsync(Guid organizationId, bool onlyActive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie zespoły, których członkiem jest dany uczestnik.
    /// </summary>
    Task<IReadOnlyList<Team>> GetByMemberAsync(Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie zespoły przypisane do danej grupy.
    /// </summary>
    Task<IReadOnlyList<Team>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie zespoły prowadzone przez danego trenera (stały trener).
    /// </summary>
    Task<IReadOnlyList<Team>> GetByTrainerAsync(Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy uczestnik jest już w danym zespole.
    /// </summary>
    Task<bool> IsMemberInTeamAsync(Guid teamId, Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy trener jest już stałym trenerem danego zespołu.
    /// </summary>
    Task<bool> IsTrainerAssignedAsync(Guid teamId, Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca liczbę zespołów w organizacji.
    /// </summary>
    Task<int> CountByOrganizationAsync(Guid organizationId, bool activeOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy zespół ma aktywne zapisy na nadchodzące zajęcia.
    /// </summary>
    Task<bool> HasActiveEnrollmentsAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dodaje uczestnika do zespołu.
    /// </summary>
    Task AddMemberAsync(Guid teamId, Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa uczestnika z zespołu.
    /// </summary>
    Task RemoveMemberAsync(Guid teamId, Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dodaje stałego trenera do zespołu.
    /// </summary>
    Task AddTrainerAsync(Guid teamId, Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa stałego trenera z zespołu.
    /// </summary>
    Task RemoveTrainerAsync(Guid teamId, Guid trainerMemberId, CancellationToken cancellationToken = default);
}
