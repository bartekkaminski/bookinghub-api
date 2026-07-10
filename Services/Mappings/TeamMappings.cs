using BookingHub.Api.Dtos.Team;
using BookingHub.Api.Models;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services.Mappings;

internal static class TeamMappings
{
    public static TeamSummaryResponse ToSummary(this Team team) => new()
    {
        Id             = team.Id,
        OrganizationId = team.OrganizationId,
        Name           = team.Name,
        IsActive       = team.IsActive,
        Priority       = team.Priority,
        MembersCount   = team.Members.Count,
        MemberNames    = team.Members
            .Select(tm => tm.OrganizationMember?.ResolveDisplayName() ?? string.Empty)
            .Where(n => n.Length > 0)
            .ToList(),
    };

    public static TeamDetailResponse ToDetail(this Team team) => new()
    {
        Id             = team.Id,
        OrganizationId = team.OrganizationId,
        Name           = team.Name,
        IsActive       = team.IsActive,
        Priority       = team.Priority,
        Notes          = team.Notes,
        Members        = team.Members.Select(tm => new TeamMemberInfo
        {
            MemberId    = tm.OrganizationMemberId,
            PersonId    = tm.OrganizationMember?.PersonId ?? Guid.Empty,
            DisplayName = tm.OrganizationMember?.ResolveDisplayName() ?? string.Empty,
            PhotoUrl    = tm.OrganizationMember?.PhotoUrl ?? tm.OrganizationMember?.Person?.PhotoUrl,
            Color       = tm.OrganizationMember?.Color,
        }).ToList(),
        Groups = team.Groups.Select(tg => new TeamGroupInfo
        {
            GroupId   = tg.GroupId,
            GroupName = tg.Group?.Name ?? string.Empty,
            Color     = tg.Group?.Color,
        }).ToList(),
        Trainers = team.Trainers.Select(tt => new TeamTrainerInfo
        {
            TrainerMemberId = tt.TrainerMemberId,
            DisplayName     = tt.Trainer?.ResolveDisplayName() ?? string.Empty,
            Color           = tt.Trainer?.Color,
        }).ToList(),
        CreatedAt = team.CreatedAt,
        UpdatedAt = team.UpdatedAt,
    };

    public static Team ToEntity(this CreateTeamRequest dto, Guid organizationId) => new()
    {
        OrganizationId = organizationId,
        Name           = dto.Name?.Trim(),
        Priority       = dto.Priority,
        Notes          = dto.Notes?.Trim(),
        IsActive       = true,
    };

    public static void ApplyUpdate(this Team team, UpdateTeamRequest dto)
    {
        team.Name     = dto.Name?.Trim();
        team.Priority = dto.Priority;
        team.Notes    = dto.Notes?.Trim();
        team.IsActive = dto.IsActive;
    }
}
