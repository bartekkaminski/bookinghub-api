using System.ComponentModel.DataAnnotations;
using BookingHub.Api.Models;

namespace BookingHub.Api.Dtos.Enrollment;

/// <summary>Skrócone dane zapisu indywidualnego — do list.</summary>
public sealed class EnrollmentSummaryResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }
    public Guid OrganizationMemberId { get; set; }
    public string MemberDisplayName { get; set; } = string.Empty;
    public EventEnrollmentStatus Status { get; set; }
    public bool HasPendingCancellation { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Pełne dane zapisu indywidualnego — widok szczegółowy.</summary>
public sealed class EnrollmentDetailResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid OrganizationId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }
    public DateTime EventEndTime { get; set; }
    public Guid OrganizationMemberId { get; set; }
    public string MemberDisplayName { get; set; } = string.Empty;
    public EventEnrollmentStatus Status { get; set; }
    public IReadOnlyList<CancellationRequestInfo> CancellationRequests { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public sealed class CancellationRequestInfo
{
    public Guid Id { get; set; }
    public string? Reason { get; set; }
    public DateTime RequestedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>Skrócone dane zapisu zespołu — do list.</summary>
public sealed class TeamEnrollmentSummaryResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }
    public Guid TeamId { get; set; }
    public string? TeamName { get; set; }
    public EventEnrollmentStatus Status { get; set; }
    public int MembersCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Żądanie zapisu indywidualnego uczestnika na zajęcia.</summary>
public sealed class EnrollMemberRequest
{
    [Required]
    public Guid OrganizationMemberId { get; set; }
}

/// <summary>Żądanie zapisu całego zespołu na zajęcia.</summary>
public sealed class EnrollTeamRequest
{
    [Required]
    public Guid TeamId { get; set; }
}

/// <summary>Żądanie zmiany statusu zapisu (np. oznaczenie obecności).</summary>
public sealed class SetEnrollmentStatusRequest
{
    [Required]
    public EventEnrollmentStatus Status { get; set; }
}

/// <summary>Zbiorcze oznaczanie obecności na zajęciach.</summary>
public sealed class BulkAttendanceRequest
{
    /// <summary>Lista Id zapisów (EventEnrollment) które mają być oznaczone jako Attended.</summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<Guid> EnrollmentIds { get; set; } = [];
}
