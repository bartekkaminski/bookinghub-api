using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium grup zajęciowych.
/// </summary>
public interface IGroupRepository : IBaseRepository<Group>
{
    /// <summary>
    /// Pobiera grupę z listą członków (GroupMember → OrganizationMember → Person).
    /// </summary>
    Task<Group?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera grupę z pełnymi danymi — Members, Teams, CostRates.
    /// </summary>
    Task<Group?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę grup z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<Group>> GetPagedAsync(GroupFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie aktywne grupy w organizacji.
    /// </summary>
    Task<IReadOnlyList<Group>> GetByOrganizationAsync(Guid organizationId, bool onlyActive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie grupy, do których przypisany jest dany uczestnik.
    /// </summary>
    Task<IReadOnlyList<Group>> GetByMemberAsync(Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie grupy, do których przypisany jest dany zespół.
    /// </summary>
    Task<IReadOnlyList<Group>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie grupy, do których przypisany jest dany trener.
    /// </summary>
    Task<IReadOnlyList<Group>> GetByTrainerAsync(Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy nazwa grupy jest już zajęta w organizacji.
    /// </summary>
    Task<bool> IsNameTakenInOrgAsync(Guid organizationId, string name, Guid? excludeGroupId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy uczestnik jest już przypisany do grupy.
    /// </summary>
    Task<bool> IsMemberInGroupAsync(Guid groupId, Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy zespół jest już przypisany do grupy.
    /// </summary>
    Task<bool> IsTeamInGroupAsync(Guid groupId, Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy trener jest już przypisany do grupy.
    /// </summary>
    Task<bool> IsTrainerAssignedAsync(Guid groupId, Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy nazwa grupy jest już zajęta w organizacji.
    /// Alias spójny z konwencją usług (IsNameTakenAsync).
    /// </summary>
    Task<bool> IsNameTakenAsync(Guid organizationId, string name, Guid? excludeGroupId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy grupa ma zaplanowane zajęcia w przyszłości.
    /// </summary>
    Task<bool> HasUpcomingEventsAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dodaje uczestnika do grupy.
    /// </summary>
    Task AddMemberAsync(Guid groupId, Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa uczestnika z grupy.
    /// </summary>
    Task RemoveMemberAsync(Guid groupId, Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dodaje zespół do grupy.
    /// </summary>
    Task AddTeamAsync(Guid groupId, Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa zespół z grupy.
    /// </summary>
    Task RemoveTeamAsync(Guid groupId, Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Przypisuje stałego trenera do grupy.
    /// </summary>
    Task AddTrainerAsync(Guid groupId, Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa przypisanie trenera do grupy.
    /// </summary>
    Task RemoveTrainerAsync(Guid groupId, Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca liczbę grup w organizacji.
    /// </summary>
    Task<int> CountByOrganizationAsync(Guid organizationId, bool activeOnly = false, CancellationToken cancellationToken = default);
}
