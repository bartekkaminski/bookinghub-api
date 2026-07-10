using BookingHub.Api.Dtos.Cancellation;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class CancellationMappings
{
    public static CancellationRequestSummaryResponse ToSummary(this CancellationRequest cr) => new()
    {
        Id                   = cr.Id,
        EventEnrollmentId    = cr.EventEnrollmentId,
        EventId              = cr.EventEnrollment?.EventId ?? Guid.Empty,
        EventTitle           = cr.EventEnrollment?.Event?.Title ?? string.Empty,
        EventStartTime       = cr.EventEnrollment?.Event?.StartTime ?? DateTime.MinValue,
        RequestedByMemberId  = cr.RequestedByMemberId,
        RequestedByName      = cr.RequestedBy?.ResolveDisplayName() ?? string.Empty,
        Reason               = cr.Reason,
        RequestedAt          = cr.RequestedAt,
        Status               = cr.Status,
    };

    public static CancellationRequestDetailResponse ToDetail(this CancellationRequest cr) => new()
    {
        Id                  = cr.Id,
        EventEnrollmentId   = cr.EventEnrollmentId,
        EventId             = cr.EventEnrollment?.EventId ?? Guid.Empty,
        OrganizationId      = cr.EventEnrollment?.Event?.OrganizationId ?? Guid.Empty,
        EventTitle          = cr.EventEnrollment?.Event?.Title ?? string.Empty,
        EventStartTime      = cr.EventEnrollment?.Event?.StartTime ?? DateTime.MinValue,
        RequestedByMemberId = cr.RequestedByMemberId,
        RequestedByName     = cr.RequestedBy?.ResolveDisplayName() ?? string.Empty,
        Reason              = cr.Reason,
        RequestedAt         = cr.RequestedAt,
        Status              = cr.Status,
        ReviewedByPersonId  = cr.ReviewedByPersonId,
        ReviewedByName      = cr.ReviewedBy is not null
            ? $"{cr.ReviewedBy.FirstName} {cr.ReviewedBy.LastName}".Trim()
            : null,
        ReviewedAt          = cr.ReviewedAt,
        ReviewNote          = cr.ReviewNote,
        CreatedAt           = cr.CreatedAt,
    };
}
