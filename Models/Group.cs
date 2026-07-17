namespace BookingHub.Api.Models;

public class Group : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Dowolna nazwa grupy, np. "Dorośli", "Dzieci 6-8 lat", "Zaawansowani"</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Kolor hex grupy w kalendarzu, np. "#F5A623". Null = domyślny szary.</summary>
    public string? Color { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Admin który utworzył grupę</summary>
    public Guid? CreatedByPersonId { get; set; }

    public Organization Organization { get; set; } = null!;
    public Person? CreatedBy { get; set; }
    public ICollection<GroupMember> Members { get; set; } = [];
    public ICollection<TeamGroup> Teams { get; set; } = [];
    public ICollection<GroupTrainer> Trainers { get; set; } = [];
    public ICollection<Event> Events { get; set; } = [];
    public ICollection<GroupCostRate> CostRates { get; set; } = [];
}
