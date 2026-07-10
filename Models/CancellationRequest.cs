namespace BookingHub.Api.Models;

public class CancellationRequest : BaseEntity
{
    public Guid EventEnrollmentId { get; set; }

    /// <summary>Uczestnik który składa wniosek o odwołanie</summary>
    public Guid RequestedByMemberId { get; set; }

    /// <summary>Powód odwołania podany przez uczestnika, np. "choroba", "wyjazd"</summary>
    public string? Reason { get; set; }

    /// <summary>Kiedy złożono wniosek (UTC)</summary>
    public DateTime RequestedAt { get; set; }

    public CancellationStatus Status { get; set; } = CancellationStatus.Pending;

    /// <summary>Trener lub admin który rozpatrzył wniosek</summary>
    public Guid? ReviewedByPersonId { get; set; }

    /// <summary>Kiedy wniosek został rozpatrzony (UTC)</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Opcjonalna notatka trenera, np. powód odrzucenia</summary>
    public string? ReviewNote { get; set; }

    public Guid? CreatedByPersonId { get; set; }

    public EventEnrollment EventEnrollment { get; set; } = null!;
    public OrganizationMember RequestedBy { get; set; } = null!;
    public Person? ReviewedBy { get; set; }
    public Person? CreatedBy { get; set; }
}
