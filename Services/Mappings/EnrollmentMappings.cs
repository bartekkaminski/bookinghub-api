using BookingHub.Api.Dtos.Enrollment;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class EnrollmentMappings
{
    public static EnrollmentSummaryResponse ToSummary(this EventEnrollment enrollment) => new()
    {
        Id                      = enrollment.Id,
        EventId                 = enrollment.EventId,
        EventTitle              = enrollment.Event?.Title ?? string.Empty,
        EventStartTime          = enrollment.Event?.StartTime ?? DateTime.MinValue,
        OrganizationMemberId    = enrollment.OrganizationMemberId,
        MemberDisplayName       = enrollment.OrganizationMember?.ResolveDisplayName() ?? string.Empty,
        Status                  = enrollment.Status,
        HasPendingCancellation  = enrollment.CancellationRequests.Any(cr => cr.Status == CancellationStatus.Pending),
        CreatedAt               = enrollment.CreatedAt,
    };

    public static EnrollmentDetailResponse ToDetail(this EventEnrollment enrollment) => new()
    {
        Id                   = enrollment.Id,
        EventId              = enrollment.EventId,
        OrganizationId       = enrollment.Event?.OrganizationId ?? Guid.Empty,
        EventTitle           = enrollment.Event?.Title ?? string.Empty,
        EventStartTime       = enrollment.Event?.StartTime ?? DateTime.MinValue,
        EventEndTime         = enrollment.Event?.EndTime ?? DateTime.MinValue,
        OrganizationMemberId = enrollment.OrganizationMemberId,
        MemberDisplayName    = enrollment.OrganizationMember?.ResolveDisplayName() ?? string.Empty,
        Status               = enrollment.Status,
        CancellationRequests = enrollment.CancellationRequests.Select(cr => new CancellationRequestInfo
        {
            Id         = cr.Id,
            Reason     = cr.Reason,
            RequestedAt= cr.RequestedAt,
            Status     = cr.Status.ToString(),
            ReviewNote = cr.ReviewNote,
            ReviewedAt = cr.ReviewedAt,
        }).ToList(),
        CreatedAt = enrollment.CreatedAt,
    };

    public static TeamEnrollmentSummaryResponse ToSummary(this EventTeamEnrollment enrollment) => new()
    {
        Id             = enrollment.Id,
        EventId        = enrollment.EventId,
        EventTitle     = enrollment.Event?.Title ?? string.Empty,
        EventStartTime = enrollment.Event?.StartTime ?? DateTime.MinValue,
        TeamId         = enrollment.TeamId,
        TeamName       = enrollment.Team?.Name,
        Status         = enrollment.Status,
        MembersCount   = enrollment.Team?.Members.Count ?? 0,
        CreatedAt      = enrollment.CreatedAt,
    };
}
