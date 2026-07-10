using BookingHub.Api.Dtos.Location;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class LocationMappings
{
    public static LocationSummaryResponse ToSummary(this Location location) => new()
    {
        Id             = location.Id,
        OrganizationId = location.OrganizationId,
        Name           = location.Name,
        Address        = location.Address,
        IsActive       = location.IsActive,
    };

    public static LocationDetailResponse ToDetail(this Location location) => new()
    {
        Id             = location.Id,
        OrganizationId = location.OrganizationId,
        Name           = location.Name,
        Address        = location.Address,
        Description    = location.Description,
        IsActive       = location.IsActive,
        CreatedAt      = location.CreatedAt,
        UpdatedAt      = location.UpdatedAt,
    };

    public static Location ToEntity(this CreateLocationRequest dto, Guid organizationId) => new()
    {
        OrganizationId = organizationId,
        Name           = dto.Name.Trim(),
        Address        = dto.Address?.Trim(),
        Description    = dto.Description?.Trim(),
        IsActive       = true,
    };

    public static void ApplyUpdate(this Location location, UpdateLocationRequest dto)
    {
        location.Name        = dto.Name.Trim();
        location.Address     = dto.Address?.Trim();
        location.Description = dto.Description?.Trim();
        location.IsActive    = dto.IsActive;
    }
}
