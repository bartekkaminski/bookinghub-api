namespace BookingHub.Api.Models;

public class EventEnrollment : BaseEntity
{
    public Guid EventId { get; set; }

    /// <summary>Uczestnik zapisany indywidualnie — OrganizationMember z Role=Participant.</summary>
    public Guid OrganizationMemberId { get; set; }

    public EventEnrollmentStatus Status { get; set; } = EventEnrollmentStatus.Enrolled;
    public Guid? CreatedByPersonId { get; set; }

    public Event Event { get; set; } = null!;
    public OrganizationMember OrganizationMember { get; set; } = null!;
    public Person? CreatedBy { get; set; }

    /// <summary>Historia wniosków o odwołanie tego zapisu</summary>
    public ICollection<CancellationRequest> CancellationRequests { get; set; } = [];
}
