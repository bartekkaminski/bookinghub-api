using BookingHub.Api.Dtos.Discipline;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class DisciplineMappings
{
    public static DisciplineSummaryResponse ToSummary(this Discipline discipline, int rankCount) => new()
    {
        Id             = discipline.Id,
        OrganizationId = discipline.OrganizationId,
        Name           = discipline.Name,
        Color          = discipline.Color,
        RankCount      = rankCount,
    };

    public static DisciplineDetailResponse ToDetail(this Discipline discipline, int rankCount) => new()
    {
        Id             = discipline.Id,
        OrganizationId = discipline.OrganizationId,
        Name           = discipline.Name,
        Color          = discipline.Color,
        RankCount      = rankCount,
        CreatedAt      = discipline.CreatedAt,
        UpdatedAt      = discipline.UpdatedAt,
    };

    public static void ApplyUpdate(this Discipline discipline, UpdateDisciplineRequest dto)
    {
        discipline.Name  = dto.Name.Trim();
        discipline.Color = dto.Color?.Trim();
    }
}
