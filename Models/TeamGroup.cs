namespace BookingHub.Api.Models;

public class TeamGroup : BaseEntity
{
    public Guid TeamId { get; set; }
    public Guid GroupId { get; set; }

    /// <summary>Admin który przypisał skład do grupy</summary>
    public Guid? CreatedByPersonId { get; set; }

    public Team Team { get; set; } = null!;
    public Group Group { get; set; } = null!;
    public Person? CreatedBy { get; set; }
}
