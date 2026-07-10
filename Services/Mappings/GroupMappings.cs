using BookingHub.Api.Dtos.Group;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class GroupMappings
{
    public static GroupSummaryResponse ToSummary(this Group group) => new()
    {
        Id                  = group.Id,
        OrganizationId      = group.OrganizationId,
        Name                = group.Name,
        Description         = group.Description,
        Color               = group.Color,
        IsActive            = group.IsActive,
        MembersCount        = group.Members.Count,
        TeamsCount          = group.Teams.Count,
        CurrentMonthlyCost  = group.CostRates.FirstOrDefault(r => r.ValidTo is null)?.MonthlyCost,
        CurrentCostCurrency = group.CostRates.FirstOrDefault(r => r.ValidTo is null)?.Currency,
    };

    public static GroupDetailResponse ToDetail(this Group group) => new()
    {
        Id             = group.Id,
        OrganizationId = group.OrganizationId,
        Name           = group.Name,
        Description    = group.Description,
        Color          = group.Color,
        IsActive       = group.IsActive,
        Members        = group.Members.Select(gm => new GroupMemberInfo
        {
            MemberId    = gm.OrganizationMemberId,
            PersonId    = gm.OrganizationMember?.PersonId ?? Guid.Empty,
            DisplayName = gm.OrganizationMember?.ResolveDisplayName() ?? string.Empty,
            PhotoUrl    = gm.OrganizationMember?.PhotoUrl ?? gm.OrganizationMember?.Person?.PhotoUrl,
            Color       = gm.OrganizationMember?.Color,
            Priority    = gm.OrganizationMember?.Priority,
            JoinedAt    = gm.CreatedAt,
        }).ToList(),
        Teams = group.Teams.Select(tg => new GroupTeamInfo
        {
            TeamId       = tg.TeamId,
            TeamName     = tg.Team?.Name,
            Priority     = tg.Team?.Priority,
            MembersCount = tg.Team?.Members.Count ?? 0,
        }).ToList(),
        CostRates = group.CostRates.OrderByDescending(r => r.ValidFrom).Select(r => new GroupCostRateInfo
        {
            Id          = r.Id,
            MonthlyCost = r.MonthlyCost,
            Currency    = r.Currency,
            ValidFrom   = r.ValidFrom,
            ValidTo     = r.ValidTo,
        }).ToList(),
        CreatedAt = group.CreatedAt,
        UpdatedAt = group.UpdatedAt,
    };

    public static Group ToEntity(this CreateGroupRequest dto, Guid organizationId) => new()
    {
        OrganizationId = organizationId,
        Name           = dto.Name.Trim(),
        Description    = dto.Description?.Trim(),
        Color          = dto.Color?.Trim(),
        IsActive       = true,
    };

    public static void ApplyUpdate(this Group group, UpdateGroupRequest dto)
    {
        group.Name        = dto.Name.Trim();
        group.Description = dto.Description?.Trim();
        group.Color       = dto.Color?.Trim();
        group.IsActive    = dto.IsActive;
    }
}
