namespace BookingHub.Api.Models;

public class GroupMember : BaseEntity
{
    public Guid GroupId { get; set; }

    /// <summary>
    /// Uczestnik przypisany do grupy — przez OrganizationMember, nie Person,
    /// bo przynależność do grupy jest zawsze w kontekście organizacji.
    /// </summary>
    public Guid OrganizationMemberId { get; set; }

    /// <summary>Admin który przypisał uczestnika do grupy</summary>
    public Guid? CreatedByPersonId { get; set; }

    public DateTime JoinedAt { get; set; }

    public Group Group { get; set; } = null!;
    public OrganizationMember OrganizationMember { get; set; } = null!;
    public Person? CreatedBy { get; set; }
}
