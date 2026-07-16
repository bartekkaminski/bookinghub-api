namespace BookingHub.Api.Models;

public class GroupTrainer : BaseEntity
{
    /// <summary>Grupa zajęciowa</summary>
    public Guid GroupId { get; set; }

    /// <summary>Stały trener grupy (OrganizationMember z Role=Trainer)</summary>
    public Guid TrainerMemberId { get; set; }

    public Guid? CreatedByPersonId { get; set; }

    public Group Group { get; set; } = null!;
    public OrganizationMember Trainer { get; set; } = null!;
    public Person? CreatedBy { get; set; }
}
