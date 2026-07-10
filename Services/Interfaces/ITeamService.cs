using BookingHub.Api.Dtos.Team;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania zespołami i ich składem.
/// </summary>
public interface ITeamService
{
    /// <summary>Pobiera stronicowaną listę zespołów w organizacji.</summary>
    Task<PagedResult<TeamSummaryResponse>> GetPagedAsync(Guid organizationId, TeamFilterParams filter, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły zespołu wraz z listą członków, trenerów i grup.</summary>
    Task<TeamDetailResponse> GetByIdAsync(Guid teamId, CancellationToken ct = default);

    /// <summary>Pobiera wszystkie aktywne zespoły organizacji (do selectów).</summary>
    Task<IReadOnlyList<TeamSummaryResponse>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Tworzy nowy zespół.</summary>
    Task<TeamDetailResponse> CreateAsync(Guid organizationId, CreateTeamRequest request, CancellationToken ct = default);

    /// <summary>Aktualizuje dane zespołu.</summary>
    Task<TeamDetailResponse> UpdateAsync(Guid teamId, UpdateTeamRequest request, CancellationToken ct = default);

    /// <summary>Usuwa zespół (soft delete). Tylko gdy brak aktywnych zapisów na zajęcia.</summary>
    Task DeleteAsync(Guid teamId, CancellationToken ct = default);

    /// <summary>Dodaje uczestnika do zespołu.</summary>
    Task AddMemberAsync(Guid teamId, Guid organizationMemberId, CancellationToken ct = default);

    /// <summary>Usuwa uczestnika z zespołu.</summary>
    Task RemoveMemberAsync(Guid teamId, Guid organizationMemberId, CancellationToken ct = default);

    /// <summary>Przypisuje stałego trenera do zespołu.</summary>
    Task AssignTrainerAsync(Guid teamId, Guid trainerMemberId, CancellationToken ct = default);

    /// <summary>Usuwa przypisanie trenera do zespołu.</summary>
    Task RemoveTrainerAsync(Guid teamId, Guid trainerMemberId, CancellationToken ct = default);
}
