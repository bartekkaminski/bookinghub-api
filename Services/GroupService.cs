using BookingHub.Api.Dtos.Cost;
using BookingHub.Api.Dtos.Group;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania grupami zajęciowymi i ich składem.
/// </summary>
public sealed class GroupService : IGroupService
{
    private readonly IGroupRepository _groups;
    private readonly IOrganizationRepository _organizations;
    private readonly IOrganizationMemberRepository _members;
    private readonly ITeamRepository _teams;
    private readonly IGroupCostRateRepository _costRates;
    private readonly ILogger<GroupService> _logger;

    public GroupService(
        IGroupRepository groups,
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        ITeamRepository teams,
        IGroupCostRateRepository costRates,
        ILogger<GroupService> logger)
    {
        _groups        = groups;
        _organizations = organizations;
        _members       = members;
        _teams         = teams;
        _costRates     = costRates;
        _logger        = logger;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<GroupSummaryResponse>> GetPagedAsync(Guid organizationId, GroupFilterParams filter, CancellationToken ct = default)
    {
        filter.OrganizationId = organizationId;
        var paged = await _groups.GetPagedAsync(filter, ct);
        return paged.Map(g => g.ToSummary());
    }

    /// <inheritdoc/>
    public async Task<GroupDetailResponse> GetByIdAsync(Guid groupId, CancellationToken ct = default)
    {
        var group = await _groups.GetWithDetailsAsync(groupId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Grupa {groupId} nie istnieje.");
        return group.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GroupSummaryResponse>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var groups = await _groups.GetByOrganizationAsync(organizationId, onlyActive: true, ct);
        return groups.Select(g => g.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<GroupDetailResponse> CreateAsync(Guid organizationId, CreateGroupRequest request, CancellationToken ct = default)
    {
        var orgExists = await _organizations.ExistsAsync(organizationId, ct);
        if (!orgExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {organizationId} nie istnieje.");

        if (await _groups.IsNameTakenInOrgAsync(organizationId, request.Name, null, ct))
            throw new ServiceException(ServiceErrorCode.GroupNameTaken,
                $"Nazwa grupy '{request.Name}' jest już zajęta w tej organizacji.", nameof(request.Name));

        var entity  = request.ToEntity(organizationId);
        var created = await _groups.AddAsync(entity, ct);
        var details = await _groups.GetWithDetailsAsync(created.Id, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<GroupDetailResponse> UpdateAsync(Guid groupId, UpdateGroupRequest request, CancellationToken ct = default)
    {
        var group = await _groups.GetByIdAsync(groupId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Grupa {groupId} nie istnieje.");

        if (!string.Equals(group.Name, request.Name, StringComparison.OrdinalIgnoreCase) &&
            await _groups.IsNameTakenInOrgAsync(group.OrganizationId, request.Name, excludeGroupId: groupId, ct))
            throw new ServiceException(ServiceErrorCode.GroupNameTaken,
                $"Nazwa grupy '{request.Name}' jest już zajęta w tej organizacji.", nameof(request.Name));

        group.ApplyUpdate(request);
        await _groups.UpdateAsync(group, ct);

        var details = await _groups.GetWithDetailsAsync(groupId, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid groupId, CancellationToken ct = default)
    {
        var group = await _groups.GetByIdAsync(groupId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Grupa {groupId} nie istnieje.");

        var hasUpcomingEvents = await _groups.HasUpcomingEventsAsync(groupId, ct);
        if (hasUpcomingEvents)
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Nie można usunąć grupy z zaplanowanymi zajęciami.");

        await _groups.DeleteAsync(groupId, ct);
    }

    /// <inheritdoc/>
    public async Task AddMemberAsync(Guid groupId, Guid organizationMemberId, CancellationToken ct = default)
    {
        var group = await _groups.GetByIdAsync(groupId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Grupa {groupId} nie istnieje.");

        var member = await _members.GetByIdAsync(organizationMemberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Członek {organizationMemberId} nie istnieje.");

        if (member.OrganizationId != group.OrganizationId)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Członek należy do innej organizacji niż grupa.");

        var alreadyIn = await _groups.IsMemberInGroupAsync(groupId, organizationMemberId, ct);
        if (alreadyIn)
            throw new ServiceException(ServiceErrorCode.MemberAlreadyInGroup,
                "Uczestnik jest już w tej grupie.");

        await _groups.AddMemberAsync(groupId, organizationMemberId, ct);
    }

    /// <inheritdoc/>
    public async Task RemoveMemberAsync(Guid groupId, Guid organizationMemberId, CancellationToken ct = default)
    {
        var inGroup = await _groups.IsMemberInGroupAsync(groupId, organizationMemberId, ct);
        if (!inGroup)
            throw new ServiceException(ServiceErrorCode.NotFound,
                "Uczestnik nie jest członkiem tej grupy.");

        await _groups.RemoveMemberAsync(groupId, organizationMemberId, ct);
    }

    /// <inheritdoc/>
    public async Task AddTeamAsync(Guid groupId, Guid teamId, CancellationToken ct = default)
    {
        var group = await _groups.GetByIdAsync(groupId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Grupa {groupId} nie istnieje.");

        var team = await _teams.GetByIdAsync(teamId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zespół {teamId} nie istnieje.");

        if (team.OrganizationId != group.OrganizationId)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Zespół należy do innej organizacji niż grupa.");

        var alreadyIn = await _groups.IsTeamInGroupAsync(groupId, teamId, ct);
        if (alreadyIn)
            throw new ServiceException(ServiceErrorCode.TeamAlreadyInGroup,
                "Zespół jest już przypisany do tej grupy.");

        await _groups.AddTeamAsync(groupId, teamId, ct);
    }

    /// <inheritdoc/>
    public async Task RemoveTeamAsync(Guid groupId, Guid teamId, CancellationToken ct = default)
    {
        var inGroup = await _groups.IsTeamInGroupAsync(groupId, teamId, ct);
        if (!inGroup)
            throw new ServiceException(ServiceErrorCode.NotFound,
                "Zespół nie jest przypisany do tej grupy.");

        await _groups.RemoveTeamAsync(groupId, teamId, ct);
    }

    // ── Stawki grupy ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GroupCostRateResponse>> GetCostRatesAsync(Guid groupId, CancellationToken ct = default)
    {
        var exists = await _groups.ExistsAsync(groupId, ct);
        if (!exists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Grupa {groupId} nie istnieje.");

        var rates = await _costRates.GetByGroupAsync(groupId, ct);
        return rates.Select(r => r.ToResponse()).ToList();
    }

    /// <inheritdoc/>
    public async Task<GroupCostRateResponse> AddCostRateAsync(Guid groupId, AddGroupCostRateRequest request, CancellationToken ct = default)
    {
        var exists = await _groups.ExistsAsync(groupId, ct);
        if (!exists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Grupa {groupId} nie istnieje.");

        // Zamknij aktywną stawkę jeśli istnieje
        var active = await _costRates.GetCurrentByGroupAsync(groupId, ct);
        if (active is not null)
        {
            var autoClose = request.ValidFrom.AddDays(-1);
            if (autoClose < active.ValidFrom)
                throw new ServiceException(ServiceErrorCode.InvalidRateDateRange,
                    "Data ValidFrom nowej stawki musi być późniejsza niż ValidFrom poprzedniej.");

            active.ValidTo = autoClose;
            await _costRates.UpdateAsync(active, ct);
        }

        var entity  = request.ToEntity(groupId);
        var created = await _costRates.AddAsync(entity, ct);
        return created.ToResponse();
    }

    /// <inheritdoc/>
    public async Task<GroupCostRateResponse> CloseCostRateAsync(Guid rateId, CloseGroupCostRateRequest request, CancellationToken ct = default)
    {
        var rate = await _costRates.GetByIdAsync(rateId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Stawka {rateId} nie istnieje.");

        if (rate.ValidTo is not null)
            throw new ServiceException(ServiceErrorCode.Conflict, "Stawka jest już zamknięta.");

        if (request.ValidTo < rate.ValidFrom)
            throw new ServiceException(ServiceErrorCode.InvalidRateDateRange,
                "ValidTo musi być >= ValidFrom.", nameof(request.ValidTo));

        rate.ValidTo = request.ValidTo;
        await _costRates.UpdateAsync(rate, ct);
        return rate.ToResponse();
    }

    /// <inheritdoc/>
    public async Task DeleteCostRateAsync(Guid rateId, CancellationToken ct = default)
    {
        var rate = await _costRates.GetByIdAsync(rateId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Stawka {rateId} nie istnieje.");

        if (rate.ValidTo is not null)
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Nie można usunąć zamkniętej stawki — zachuj ją do celów historycznych.");

        await _costRates.DeleteAsync(rateId, ct);
    }
}
