using BookingHub.Api.Dtos.Cost;
using BookingHub.Api.Dtos.Group;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania grupami zajęciowymi i ich składem.
/// </summary>
public interface IGroupService
{
    /// <summary>Pobiera stronicowaną listę grup w organizacji.</summary>
    Task<PagedResult<GroupSummaryResponse>> GetPagedAsync(Guid organizationId, GroupFilterParams filter, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły grupy wraz z listą członków, zespołów i stawkami.</summary>
    Task<GroupDetailResponse> GetByIdAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>Pobiera wszystkie aktywne grupy organizacji (do selectów).</summary>
    Task<IReadOnlyList<GroupSummaryResponse>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Pobiera grupy, do których dany członek jest przypisany jako trener.</summary>
    Task<IReadOnlyList<GroupSummaryResponse>> GetByTrainerAsync(Guid trainerMemberId, CancellationToken ct = default);

    /// <summary>Tworzy nową grupę zajęciową.</summary>
    Task<GroupDetailResponse> CreateAsync(Guid organizationId, CreateGroupRequest request, CancellationToken ct = default);

    /// <summary>Aktualizuje dane grupy.</summary>
    Task<GroupDetailResponse> UpdateAsync(Guid groupId, UpdateGroupRequest request, CancellationToken ct = default);

    /// <summary>Usuwa grupę (soft delete). Tylko gdy brak aktywnych zajęć.</summary>
    Task DeleteAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>Dodaje uczestnika (OrganizationMember) do grupy.</summary>
    Task AddMemberAsync(Guid groupId, Guid organizationMemberId, CancellationToken ct = default);

    /// <summary>Usuwa uczestnika z grupy.</summary>
    Task RemoveMemberAsync(Guid groupId, Guid organizationMemberId, CancellationToken ct = default);

    /// <summary>Dodaje zespół do grupy.</summary>
    Task AddTeamAsync(Guid groupId, Guid teamId, CancellationToken ct = default);

    /// <summary>Usuwa zespół z grupy.</summary>
    Task RemoveTeamAsync(Guid groupId, Guid teamId, CancellationToken ct = default);

    /// <summary>Przypisuje stałego trenera do grupy.</summary>
    Task AssignTrainerAsync(Guid groupId, Guid trainerMemberId, CancellationToken ct = default);

    /// <summary>Usuwa przypisanie trenera do grupy.</summary>
    Task RemoveTrainerAsync(Guid groupId, Guid trainerMemberId, CancellationToken ct = default);

    // ── Stawki grupy ──────────────────────────────────────────────────────────

    /// <summary>Pobiera historię stawek miesięcznych grupy.</summary>
    Task<IReadOnlyList<GroupCostRateResponse>> GetCostRatesAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>Dodaje nową stawkę miesięczną (domyślnie zamyka poprzednią aktywną).</summary>
    Task<GroupCostRateResponse> AddCostRateAsync(Guid groupId, AddGroupCostRateRequest request, CancellationToken ct = default);

    /// <summary>Zamyka aktywną stawkę (ustawia ValidTo).</summary>
    Task<GroupCostRateResponse> CloseCostRateAsync(Guid rateId, CloseGroupCostRateRequest request, CancellationToken ct = default);

    /// <summary>Usuwa stawkę (tylko jeśli nie była jeszcze używana do rozliczeń).</summary>
    Task DeleteCostRateAsync(Guid rateId, CancellationToken ct = default);
}
