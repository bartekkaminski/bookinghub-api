namespace BookingHub.Api.Models;

public class TeamMember : BaseEntity
{
    public Guid TeamId { get; set; }

    /// <summary>
    /// Uczestnik w składzie — przez OrganizationMember, nie Person,
    /// bo przynależność do składu jest zawsze w kontekście organizacji.
    /// </summary>
    public Guid OrganizationMemberId { get; set; }

    /// <summary>Admin który dodał uczestnika do składu</summary>
    public Guid? CreatedByPersonId { get; set; }

    public Team Team { get; set; } = null!;
    public OrganizationMember OrganizationMember { get; set; } = null!;
    public Person? CreatedBy { get; set; }
}
