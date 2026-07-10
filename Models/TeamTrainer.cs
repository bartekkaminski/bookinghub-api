namespace BookingHub.Api.Models;

public class TeamTrainer : BaseEntity
{
    /// <summary>Skład (para, formacja, drużyna)</summary>
    public Guid TeamId { get; set; }

    /// <summary>Stały trener składu (OrganizationMember z Role=Trainer)</summary>
    public Guid TrainerMemberId { get; set; }

    public Guid? CreatedByPersonId { get; set; }

    public Team Team { get; set; } = null!;
    public OrganizationMember Trainer { get; set; } = null!;
    public Person? CreatedBy { get; set; }
}
