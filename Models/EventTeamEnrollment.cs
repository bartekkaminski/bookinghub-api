namespace BookingHub.Api.Models;

public class EventTeamEnrollment : BaseEntity
{
    public Guid EventId { get; set; }

    /// <summary>Skład (para, formacja, drużyna) zapisany jako jednostka.</summary>
    public Guid TeamId { get; set; }

    public EventEnrollmentStatus Status { get; set; } = EventEnrollmentStatus.Enrolled;
    public Guid? CreatedByPersonId { get; set; }

    public Event Event { get; set; } = null!;
    public Team Team { get; set; } = null!;
    public Person? CreatedBy { get; set; }
}
