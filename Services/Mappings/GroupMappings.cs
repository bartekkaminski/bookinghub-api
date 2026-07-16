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
        EffectiveMembers = group.BuildEffectiveMembers(),
        Trainers = group.Trainers.Select(gt => new GroupTrainerInfo
        {
            TrainerMemberId = gt.TrainerMemberId,
            DisplayName     = gt.Trainer?.ResolveDisplayName() ?? string.Empty,
            Color           = gt.Trainer?.Color,
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

    /// <summary>
    /// Scala bezpośrednich uczestników grupy z osobami wchodzącymi w skład przypisanych zespołów,
    /// deduplikując po OrganizationMemberId. Osoba obecna w obu ścieżkach dostaje oba oznaczenia źródła.
    /// </summary>
    private static IReadOnlyList<GroupEffectiveMemberInfo> BuildEffectiveMembers(this Group group)
    {
        var effective = new Dictionary<Guid, GroupEffectiveMemberInfo>();

        foreach (var gm in group.Members)
        {
            effective[gm.OrganizationMemberId] = new GroupEffectiveMemberInfo
            {
                MemberId            = gm.OrganizationMemberId,
                PersonId            = gm.OrganizationMember?.PersonId ?? Guid.Empty,
                DisplayName         = gm.OrganizationMember?.ResolveDisplayName() ?? string.Empty,
                PhotoUrl            = gm.OrganizationMember?.PhotoUrl ?? gm.OrganizationMember?.Person?.PhotoUrl,
                Color               = gm.OrganizationMember?.Color,
                IsDirectParticipant = true,
                TeamNames           = [],
            };
        }

        foreach (var tg in group.Teams)
        {
            if (tg.Team is null) continue;
            var teamName = tg.Team.Name ?? string.Empty;

            foreach (var tm in tg.Team.Members)
            {
                if (effective.TryGetValue(tm.OrganizationMemberId, out var existing))
                {
                    existing.TeamNames = [.. existing.TeamNames, teamName];
                    continue;
                }

                effective[tm.OrganizationMemberId] = new GroupEffectiveMemberInfo
                {
                    MemberId            = tm.OrganizationMemberId,
                    PersonId            = tm.OrganizationMember?.PersonId ?? Guid.Empty,
                    DisplayName         = tm.OrganizationMember?.ResolveDisplayName() ?? string.Empty,
                    PhotoUrl            = tm.OrganizationMember?.PhotoUrl ?? tm.OrganizationMember?.Person?.PhotoUrl,
                    Color               = tm.OrganizationMember?.Color,
                    IsDirectParticipant = false,
                    TeamNames           = [teamName],
                };
            }
        }

        return effective.Values.OrderBy(m => m.DisplayName).ToList();
    }

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
