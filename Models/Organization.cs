namespace BookingHub.Api.Models;

public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Identyfikator osoby, która utworzyła organizację.
    /// Służy do egzekwowania limitu tworzonych organizacji per użytkownik.
    /// </summary>
    public Guid? CreatedByPersonId { get; set; }
    public Person? CreatedByPerson { get; set; }

    public ICollection<OrganizationMember> Members { get; set; } = [];
    public ICollection<Group> Groups { get; set; } = [];
    public ICollection<Team> Teams { get; set; } = [];
    public ICollection<Location> Locations { get; set; } = [];
    public ICollection<Event> Events { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<Discipline> Disciplines { get; set; } = [];
}
