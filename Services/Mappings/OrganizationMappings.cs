using BookingHub.Api.Dtos.Organization;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class OrganizationMappings
{
    public static OrganizationSummaryResponse ToSummary(this Organization org, int membersCount = 0) => new()
    {
        Id           = org.Id,
        Name         = org.Name,
        Description  = org.Description,
        MembersCount = membersCount,
    };

    public static OrganizationDetailResponse ToDetail(this Organization org,
        int membersCount = 0, int activeGroupsCount = 0, int activeTeamsCount = 0) => new()
    {
        Id                = org.Id,
        Name              = org.Name,
        Description       = org.Description,
        MembersCount      = membersCount,
        ActiveGroupsCount = activeGroupsCount,
        ActiveTeamsCount  = activeTeamsCount,
        CreatedAt         = org.CreatedAt,
        UpdatedAt         = org.UpdatedAt,
    };

    public static Organization ToEntity(this CreateOrganizationRequest dto) => new()
    {
        Name        = dto.Name.Trim(),
        Description = dto.Description?.Trim(),
    };

    public static void ApplyUpdate(this Organization org, UpdateOrganizationRequest dto)
    {
        org.Name        = dto.Name.Trim();
        org.Description = dto.Description?.Trim();
    }
}
