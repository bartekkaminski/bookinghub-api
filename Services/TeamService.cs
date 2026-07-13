using BookingHub.Api.Dtos.Team;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania zespołami (parami, formacjami) i ich składem.
/// </summary>
public sealed class TeamService : ITeamService
{
    private readonly ITeamRepository _teams;
    private readonly IOrganizationRepository _organizations;
    private readonly IOrganizationMemberRepository _members;
    private readonly ILogger<TeamService> _logger;

    public TeamService(
        ITeamRepository teams,
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        ILogger<TeamService> logger)
    {
        _teams         = teams;
        _organizations = organizations;
        _members       = members;
        _logger        = logger;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<TeamSummaryResponse>> GetPagedAsync(Guid organizationId, TeamFilterParams filter, CancellationToken ct = default)
    {
        filter.OrganizationId = organizationId;
        var paged = await _teams.GetPagedAsync(filter, ct);
        return paged.Map(t => t.ToSummary());
    }

    /// <inheritdoc/>
    public async Task<TeamDetailResponse> GetByIdAsync(Guid teamId, CancellationToken ct = default)
    {
        var team = await _teams.GetWithDetailsAsync(teamId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zespół {teamId} nie istnieje.");
        return team.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TeamSummaryResponse>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var teams = await _teams.GetByOrganizationAsync(organizationId, onlyActive: true, ct);
        return teams.Select(t => t.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TeamSummaryResponse>> GetByTrainerAsync(Guid trainerMemberId, CancellationToken ct = default)
    {
        var teams = await _teams.GetByTrainerAsync(trainerMemberId, ct);
        return teams.Where(t => t.IsActive).Select(t => t.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<TeamDetailResponse> CreateAsync(Guid organizationId, CreateTeamRequest request, CancellationToken ct = default)
    {
        var orgExists = await _organizations.ExistsAsync(organizationId, ct);
        if (!orgExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {organizationId} nie istnieje.");

        var entity  = request.ToEntity(organizationId);
        var created = await _teams.AddAsync(entity, ct);
        var details = await _teams.GetWithDetailsAsync(created.Id, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<TeamDetailResponse> UpdateAsync(Guid teamId, UpdateTeamRequest request, CancellationToken ct = default)
    {
        var team = await _teams.GetByIdAsync(teamId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zespół {teamId} nie istnieje.");

        team.ApplyUpdate(request);
        await _teams.UpdateAsync(team, ct);

        var details = await _teams.GetWithDetailsAsync(teamId, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid teamId, CancellationToken ct = default)
    {
        var exists = await _teams.ExistsAsync(teamId, ct);
        if (!exists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Zespół {teamId} nie istnieje.");

        var hasActiveEnrollments = await _teams.HasActiveEnrollmentsAsync(teamId, ct);
        if (hasActiveEnrollments)
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Nie można usunąć zespołu z aktywnymi zapisami na zajęcia.");

        await _teams.DeleteAsync(teamId, ct);
    }

    /// <inheritdoc/>
    public async Task AddMemberAsync(Guid teamId, Guid organizationMemberId, CancellationToken ct = default)
    {
        var team = await _teams.GetByIdAsync(teamId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zespół {teamId} nie istnieje.");

        var member = await _members.GetByIdAsync(organizationMemberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Członek {organizationMemberId} nie istnieje.");

        if (member.OrganizationId != team.OrganizationId)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Członek należy do innej organizacji niż zespół.");

        var alreadyIn = await _teams.IsMemberInTeamAsync(teamId, organizationMemberId, ct);
        if (alreadyIn)
            throw new ServiceException(ServiceErrorCode.MemberAlreadyInTeam,
                "Uczestnik jest już w tym zespole.");

        await _teams.AddMemberAsync(teamId, organizationMemberId, ct);
    }

    /// <inheritdoc/>
    public async Task RemoveMemberAsync(Guid teamId, Guid organizationMemberId, CancellationToken ct = default)
    {
        var inTeam = await _teams.IsMemberInTeamAsync(teamId, organizationMemberId, ct);
        if (!inTeam)
            throw new ServiceException(ServiceErrorCode.NotFound,
                "Uczestnik nie jest członkiem tego zespołu.");

        await _teams.RemoveMemberAsync(teamId, organizationMemberId, ct);
    }

    /// <inheritdoc/>
    public async Task AssignTrainerAsync(Guid teamId, Guid trainerMemberId, CancellationToken ct = default)
    {
        var team = await _teams.GetByIdAsync(teamId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zespół {teamId} nie istnieje.");

        var trainer = await _members.GetWithDetailsAsync(trainerMemberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Trener {trainerMemberId} nie istnieje.");

        if (!trainer.Roles.Any(r => r.Role == MemberRole.Trainer))
            throw new ServiceException(ServiceErrorCode.NotATrainer,
                "Wskazana osoba nie ma roli Trenera.");

        if (trainer.OrganizationId != team.OrganizationId)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Trener należy do innej organizacji niż zespół.");

        var alreadyAssigned = await _teams.IsTrainerAssignedAsync(teamId, trainerMemberId, ct);
        if (alreadyAssigned)
            throw new ServiceException(ServiceErrorCode.TrainerAlreadyAssignedToTeam,
                "Trener jest już przypisany do tego zespołu.");

        await _teams.AddTrainerAsync(teamId, trainerMemberId, ct);
    }

    /// <inheritdoc/>
    public async Task RemoveTrainerAsync(Guid teamId, Guid trainerMemberId, CancellationToken ct = default)
    {
        var assigned = await _teams.IsTrainerAssignedAsync(teamId, trainerMemberId, ct);
        if (!assigned)
            throw new ServiceException(ServiceErrorCode.NotFound,
                "Trener nie jest przypisany do tego zespołu.");

        await _teams.RemoveTrainerAsync(teamId, trainerMemberId, ct);
    }
}
