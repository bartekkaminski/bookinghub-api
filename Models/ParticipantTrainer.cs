namespace BookingHub.Api.Models;

public class ParticipantTrainer : BaseEntity
{
    /// <summary>Uczestnik (OrganizationMember z Role=Participant)</summary>
    public Guid ParticipantMemberId { get; set; }

    /// <summary>Stały trener uczestnika (OrganizationMember z Role=Trainer)</summary>
    public Guid TrainerMemberId { get; set; }

    public Guid? CreatedByPersonId { get; set; }

    public OrganizationMember Participant { get; set; } = null!;
    public OrganizationMember Trainer { get; set; } = null!;
    public Person? CreatedBy { get; set; }
}
