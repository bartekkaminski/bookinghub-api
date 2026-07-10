using BookingHub.Api.Dtos.Availability;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class AvailabilityMappings
{
    public static AvailabilitySlotResponse ToResponse(this MemberAvailability slot) => new()
    {
        Id                   = slot.Id,
        OrganizationMemberId = slot.OrganizationMemberId,
        DayOfWeek            = slot.DayOfWeek,
        TimeFrom             = slot.TimeFrom,
        TimeTo               = slot.TimeTo,
        ValidFrom            = slot.ValidFrom,
        ValidTo              = slot.ValidTo,
    };

    public static MemberAvailability ToEntity(this AddAvailabilitySlotRequest dto, Guid memberId) => new()
    {
        OrganizationMemberId = memberId,
        DayOfWeek            = dto.DayOfWeek,
        TimeFrom             = dto.TimeFrom,
        TimeTo               = dto.TimeTo,
        ValidFrom            = dto.ValidFrom,
        ValidTo              = dto.ValidTo,
    };

    public static void ApplyUpdate(this MemberAvailability slot, UpdateAvailabilitySlotRequest dto)
    {
        slot.DayOfWeek = dto.DayOfWeek;
        slot.TimeFrom  = dto.TimeFrom;
        slot.TimeTo    = dto.TimeTo;
        slot.ValidFrom = dto.ValidFrom;
        slot.ValidTo   = dto.ValidTo;
    }
}
