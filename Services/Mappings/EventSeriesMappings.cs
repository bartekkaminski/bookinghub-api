using BookingHub.Api.Dtos.EventSeries;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class EventSeriesMappings
{
    public static EventSeriesSummaryResponse ToSummary(this EventSeries series) => new()
    {
        Id                  = series.Id,
        OrganizationId      = series.OrganizationId,
        Title               = series.Title,
        RecurrenceRule      = series.RecurrenceRule,
        DefaultEventType    = series.DefaultEventType,
        DefaultColor        = series.DefaultColor,
        DefaultGroupId      = series.DefaultGroupId,
        DefaultGroupName    = series.DefaultGroup?.Name,
        DefaultLocationId   = series.DefaultLocationId,
        DefaultLocationName = series.DefaultLocation?.Name,
        IsActive            = series.IsActive,
        EventsCount         = series.Events.Count,
    };

    public static EventSeriesDetailResponse ToDetail(this EventSeries series) => new()
    {
        Id                  = series.Id,
        OrganizationId      = series.OrganizationId,
        Title               = series.Title,
        Description         = series.Description,
        RecurrenceRule      = series.RecurrenceRule,
        DefaultEventType    = series.DefaultEventType,
        DefaultColor        = series.DefaultColor,
        DefaultGroupId      = series.DefaultGroupId,
        DefaultGroupName    = series.DefaultGroup?.Name,
        DefaultLocationId   = series.DefaultLocationId,
        DefaultLocationName = series.DefaultLocation?.Name,
        IsActive            = series.IsActive,
        CreatedAt           = series.CreatedAt,
        UpdatedAt           = series.UpdatedAt,
    };

    public static EventSeries ToEntity(this CreateEventSeriesRequest dto, Guid organizationId) => new()
    {
        OrganizationId   = organizationId,
        Title            = dto.Title.Trim(),
        Description      = dto.Description?.Trim(),
        RecurrenceRule   = dto.RecurrenceRule?.Trim(),
        DefaultGroupId   = dto.DefaultGroupId,
        DefaultLocationId= dto.DefaultLocationId,
        DefaultColor     = dto.DefaultColor?.Trim(),
        DefaultEventType = dto.DefaultEventType,
        IsActive         = true,
    };

    public static void ApplyUpdate(this EventSeries series, UpdateEventSeriesRequest dto)
    {
        series.Title             = dto.Title.Trim();
        series.Description       = dto.Description?.Trim();
        series.RecurrenceRule    = dto.RecurrenceRule?.Trim();
        series.DefaultGroupId    = dto.DefaultGroupId;
        series.DefaultLocationId = dto.DefaultLocationId;
        series.DefaultColor      = dto.DefaultColor?.Trim();
        series.DefaultEventType  = dto.DefaultEventType;
        series.IsActive          = dto.IsActive;
    }
}
