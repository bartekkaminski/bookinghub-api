using BookingHub.Api.Dtos.Member;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class MemberMappings
{
    /// <summary>Wyznacza wyświetlaną nazwę: DisplayName → FirstName+LastName → "(brak nazwy)".</summary>
    public static string ResolveDisplayName(this OrganizationMember member) =>
        !string.IsNullOrWhiteSpace(member.DisplayName)
            ? member.DisplayName
            : $"{member.Person?.FirstName} {member.Person?.LastName}".Trim() is { Length: > 0 } name
                ? name
                : "(brak nazwy)";

    public static MemberSummaryResponse ToSummary(this OrganizationMember member) => new()
    {
        Id             = member.Id,
        PersonId       = member.PersonId,
        OrganizationId = member.OrganizationId,
        DisplayName    = member.ResolveDisplayName(),
        PhotoUrl       = member.PhotoUrl ?? member.Person?.PhotoUrl,
        Color          = member.Color,
        Priority       = member.Priority,
        IsActive       = member.IsActive,
        Roles          = member.Roles.Select(r => r.Role).ToList(),
        HasAccount     = member.Person?.UserId is not null,
        Ranks          = member.MemberRanks.Select(mr => mr.ToInfo()).ToList(),
    };

    public static MemberDetailResponse ToDetail(this OrganizationMember member) => new()
    {
        Id               = member.Id,
        PersonId         = member.PersonId,
        OrganizationId   = member.OrganizationId,
        FirstName        = member.Person?.FirstName,
        LastName         = member.Person?.LastName,
        DisplayName      = member.DisplayName,
        PhotoUrl         = member.PhotoUrl ?? member.Person?.PhotoUrl,
        Color            = member.Color,
        Priority         = member.Priority,
        PlayerNumber     = member.PlayerNumber,
        IsActive         = member.IsActive,
        DateOfBirth      = member.Person?.DateOfBirth,
        Roles            = member.Roles.Select(r => r.Role).ToList(),
        Groups           = member.GroupMemberships.Select(gm => new MemberGroupInfo
        {
            GroupId   = gm.GroupId,
            GroupName = gm.Group?.Name ?? string.Empty,
            Color     = gm.Group?.Color,
        }).ToList(),
        Teams            = member.TeamMemberships.Select(tm => new MemberTeamInfo
        {
            TeamId   = tm.TeamId,
            TeamName = tm.Team?.Name,
            Priority = tm.Team?.Priority,
        }).ToList(),

        AssignedTrainers = member.AssignedTrainers.Select(pt => new MemberTrainerInfo
        {
            TrainerMemberId = pt.TrainerMemberId,
            DisplayName     = pt.Trainer?.ResolveDisplayName() ?? string.Empty,
            Color           = pt.Trainer?.Color,
        }).ToList(),
        Ranks      = member.MemberRanks.Select(mr => mr.ToInfo()).ToList(),
        CreatedAt  = member.CreatedAt,
        UpdatedAt  = member.UpdatedAt,
    };

    private static MemberRankInfo ToInfo(this MemberRank mr) => new()
    {
        DisciplineId   = mr.DisciplineId,
        DisciplineName = mr.Discipline?.Name ?? string.Empty,
        RankId         = mr.RankId,
        RankName       = mr.Rank?.Name ?? string.Empty,
        RankColor      = mr.Rank?.Color,
    };

    public static void ApplyUpdate(this OrganizationMember member, UpdateMemberRequest dto)
    {
        member.DisplayName   = dto.DisplayName?.Trim();
        member.PhotoUrl      = dto.PhotoUrl?.Trim();
        member.Color         = dto.Color?.Trim();
        member.Priority      = dto.Priority;
        member.PlayerNumber  = dto.PlayerNumber?.Trim();
    }

    public static void ApplyPersonUpdate(this Person person, UpdateMemberRequest dto)
    {
        if (dto.FirstName is not null) person.FirstName = dto.FirstName.Trim();
        if (dto.LastName  is not null) person.LastName  = dto.LastName.Trim();
        if (dto.DateOfBirth.HasValue)  person.DateOfBirth = dto.DateOfBirth;
    }
}
