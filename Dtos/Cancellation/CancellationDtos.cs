using System.ComponentModel.DataAnnotations;
using BookingHub.Api.Models;

namespace BookingHub.Api.Dtos.Cancellation;

/// <summary>Skrócone dane wniosku o odwołanie — do list.</summary>
public sealed class CancellationRequestSummaryResponse
{
    public Guid Id { get; set; }
    public Guid EventEnrollmentId { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }
    public Guid RequestedByMemberId { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime RequestedAt { get; set; }
    public CancellationStatus Status { get; set; }
}

/// <summary>Pełne dane wniosku o odwołanie — widok szczegółowy.</summary>
public sealed class CancellationRequestDetailResponse
{
    public Guid Id { get; set; }
    public Guid EventEnrollmentId { get; set; }
    public Guid EventId { get; set; }
    public Guid OrganizationId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }
    public Guid RequestedByMemberId { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime RequestedAt { get; set; }
    public CancellationStatus Status { get; set; }
    public Guid? ReviewedByPersonId { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Żądanie złożenia wniosku o odwołanie (przez uczestnika).</summary>
public sealed class CreateCancellationRequest
{
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>Żądanie rozpatrzenia wniosku (przez trenera / admina).</summary>
public sealed class ReviewCancellationRequest
{
    [Required]
    public CancellationStatus Decision { get; set; }

    [StringLength(500)]
    public string? ReviewNote { get; set; }
}
