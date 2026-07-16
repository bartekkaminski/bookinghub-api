using BookingHub.Api.Dtos.Rank;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class RankMappings
{
    public static RankSummaryResponse ToSummary(this OrganizationRank rank, string disciplineName, int memberCount) => new()
    {
        Id             = rank.Id,
        OrganizationId = rank.OrganizationId,
        DisciplineId   = rank.DisciplineId,
        DisciplineName = disciplineName,
        Name           = rank.Name,
        Color          = rank.Color,
        MemberCount    = memberCount,
    };

    public static RankDetailResponse ToDetail(this OrganizationRank rank, string disciplineName, int memberCount) => new()
    {
        Id             = rank.Id,
        OrganizationId = rank.OrganizationId,
        DisciplineId   = rank.DisciplineId,
        DisciplineName = disciplineName,
        Name           = rank.Name,
        Color          = rank.Color,
        MemberCount    = memberCount,
        CreatedAt      = rank.CreatedAt,
        UpdatedAt      = rank.UpdatedAt,
    };

    public static void ApplyUpdate(this OrganizationRank rank, UpdateRankRequest dto)
    {
        rank.Name  = dto.Name.Trim();
        rank.Color = dto.Color?.Trim();
    }
}
