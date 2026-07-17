namespace BookingHub.Api.Models;

public class Location : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedByPersonId { get; set; }

    public Organization Organization { get; set; } = null!;
    public Person? CreatedBy { get; set; }
    public ICollection<Event> Events { get; set; } = [];
}
