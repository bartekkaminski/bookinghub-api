using BookingHub.Api.Dtos.Cost;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class CostMappings
{
    public static GroupCostRateResponse ToResponse(this GroupCostRate rate) => new()
    {
        Id          = rate.Id,
        GroupId     = rate.GroupId,
        GroupName   = rate.Group?.Name ?? string.Empty,
        MonthlyCost = rate.MonthlyCost,
        Currency    = rate.Currency,
        ValidFrom   = rate.ValidFrom,
        ValidTo     = rate.ValidTo,
    };

    public static GroupCostRate ToEntity(this AddGroupCostRateRequest dto, Guid groupId) => new()
    {
        GroupId     = groupId,
        MonthlyCost = dto.MonthlyCost,
        Currency    = dto.Currency.Trim().ToUpperInvariant(),
        ValidFrom   = dto.ValidFrom,
        ValidTo     = null,
    };

    public static TrainerSessionRateResponse ToResponse(this TrainerSessionRate rate) => new()
    {
        Id              = rate.Id,
        TrainerMemberId = rate.TrainerMemberId,
        TrainerName     = rate.Trainer?.ResolveDisplayName() ?? string.Empty,
        RatePerHour     = rate.RatePerHour,
        Currency        = rate.Currency,
        ValidFrom       = rate.ValidFrom,
        ValidTo         = rate.ValidTo,
    };

    public static TrainerSessionRate ToEntity(this AddTrainerSessionRateRequest dto, Guid trainerMemberId) => new()
    {
        TrainerMemberId = trainerMemberId,
        RatePerHour     = dto.RatePerHour,
        Currency        = dto.Currency.Trim().ToUpperInvariant(),
        ValidFrom       = dto.ValidFrom,
        ValidTo         = null,
    };
}
