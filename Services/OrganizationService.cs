using BookingHub.Api.Dtos.Organization;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;
using BookingHub.Api.Settings;
using Microsoft.Extensions.Options;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania organizacjami.
/// </summary>
public sealed class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizations;
    private readonly IOrganizationMemberRepository _members;
    private readonly IGroupRepository _groups;
    private readonly ITeamRepository _teams;
    private readonly OrganizationLimits _limits;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IGroupRepository groups,
        ITeamRepository teams,
        IOptions<OrganizationLimits> limits,
        ILogger<OrganizationService> logger)
    {
        _organizations = organizations;
        _members       = members;
        _groups        = groups;
        _teams         = teams;
        _limits        = limits.Value;
        _logger        = logger;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<OrganizationSummaryResponse>> GetPagedAsync(OrganizationFilterParams filter, Guid? personId = null, CancellationToken ct = default)
    {
        // Wymuszenie filtrowania po PersonId — użytkownik widzi tylko swoje organizacje.
        filter.PersonId = personId;
        var paged = await _organizations.GetPagedAsync(filter, ct);
        return paged.Map(o => o.ToSummary(o.Members.Count));
    }

    /// <inheritdoc/>
    public async Task<OrganizationDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var org = await _organizations.GetWithMembersAsync(id, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {id} nie istnieje.");

        var membersCount      = await _members.CountByOrganizationAsync(id, ct);
        var activeGroupsCount = await _groups.CountByOrganizationAsync(id, activeOnly: true, ct);
        var activeTeamsCount  = await _teams.CountByOrganizationAsync(id, activeOnly: true, ct);

        return org.ToDetail(membersCount, activeGroupsCount, activeTeamsCount);
    }

    /// <inheritdoc/>
    public async Task<OrganizationDetailResponse> CreateAsync(CreateOrganizationRequest request, Guid creatorPersonId, CancellationToken ct = default)
    {
        if (_limits.MaxOrganizationsPerCreator > 0)
        {
            var createdCount = await _organizations.CountCreatedByPersonAsync(creatorPersonId, ct);
            if (createdCount >= _limits.MaxOrganizationsPerCreator)
                throw new ServiceException(
                    ServiceErrorCode.OrganizationCreationLimitReached,
                    $"Osiągnięto limit {_limits.MaxOrganizationsPerCreator} organizacji na użytkownika.");
        }

        if (await _organizations.IsNameTakenAsync(request.Name, null, ct))
            throw new ServiceException(ServiceErrorCode.OrganizationNameTaken,
                $"Nazwa organizacji '{request.Name}' jest już zajęta.", nameof(request.Name));

        var entity = request.ToEntity();
        entity.CreatedByPersonId = creatorPersonId;
        var created = await _organizations.AddAsync(entity, ct);

        // Twórca automatycznie staje się pierwszym Adminem organizacji.
        var adminMember = new OrganizationMember
        {
            OrganizationId = created.Id,
            PersonId       = creatorPersonId,
            IsActive       = true,
            Roles          = [new OrganizationMemberRole { Role = MemberRole.Admin }],
        };
        await _members.AddAsync(adminMember, ct);
        _logger.LogInformation(
            "Utworzono organizację {OrgId} ({Name}) przez PersonId={PersonId}.",
            created.Id, created.Name, creatorPersonId);

        return created.ToDetail(1, 0, 0);
    }

    /// <inheritdoc/>
    public async Task<OrganizationDetailResponse> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default)
    {
        var org = await _organizations.GetByIdAsync(id, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {id} nie istnieje.");

        if (!string.Equals(org.Name, request.Name, StringComparison.OrdinalIgnoreCase) &&
            await _organizations.IsNameTakenAsync(request.Name, id, ct))
            throw new ServiceException(ServiceErrorCode.OrganizationNameTaken,
                $"Nazwa organizacji '{request.Name}' jest już zajęta.", nameof(request.Name));

        org.ApplyUpdate(request);
        await _organizations.UpdateAsync(org, ct);

        var membersCount      = await _members.CountByOrganizationAsync(id, ct);
        var activeGroupsCount = await _groups.CountByOrganizationAsync(id, activeOnly: true, ct);
        var activeTeamsCount  = await _teams.CountByOrganizationAsync(id, activeOnly: true, ct);
        return org.ToDetail(membersCount, activeGroupsCount, activeTeamsCount);
    }

    /// <inheritdoc/>
    public async Task<OrganizationCreationLimitsResponse> GetCreationLimitsAsync(Guid personId, CancellationToken ct = default)
    {
        var max   = _limits.MaxOrganizationsPerCreator;
        var count = max > 0 ? await _organizations.CountCreatedByPersonAsync(personId, ct) : 0;
        return new OrganizationCreationLimitsResponse
        {
            MaxOrganizationsPerCreator = max,
            CreatedByMeCount           = count,
            CanCreate                  = max <= 0 || count < max,
        };
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var org = await _organizations.GetByIdAsync(id, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {id} nie istnieje.");

        var hasActiveMembers = await _members.AnyActiveByOrganizationAsync(id, ct);
        if (hasActiveMembers)
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Nie można usunąć organizacji z aktywnymi członkami.");

        await _organizations.DeleteAsync(id, ct);
        _logger.LogInformation("Usunięto organizację {OrgId} ({Name}).", id, org.Name);
    }
}
