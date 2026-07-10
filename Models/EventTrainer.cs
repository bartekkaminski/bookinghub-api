namespace BookingHub.Api.Models;

public class EventTrainer : BaseEntity
{
    public Guid EventId { get; set; }

    /// <summary>Trener prowadzący zajęcia — OrganizationMember z Role=Trainer.</summary>
    public Guid OrganizationMemberId { get; set; }

    public Guid? CreatedByPersonId { get; set; }

    public Event Event { get; set; } = null!;
    public OrganizationMember OrganizationMember { get; set; } = null!;
    public Person? CreatedBy { get; set; }
}
