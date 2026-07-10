namespace BookingHub.Api.Models;

public class Team : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Opcjonalna nazwa składu, np. "Formacja A", "Para Jan+Anna", "Drużyna Mistrzów".
    /// Null = skład bez nazwy (typowo para bez oficjalnej nazwy).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>False = skład rozwiązany. Historia zachowana w bazie.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priorytet składu ustawiony przez admina (min 1).
    /// Null = brak priorytetu. Używany przy planowaniu zajęć dla par/formacji.
    /// </summary>
    public int? Priority { get; set; }

    public string? Notes { get; set; }

    /// <summary>Admin który utworzył skład</summary>
    public Guid? CreatedByPersonId { get; set; }

    public Organization Organization { get; set; } = null!;
    public Person? CreatedBy { get; set; }
    public ICollection<TeamMember> Members { get; set; } = [];
    public ICollection<TeamGroup> Groups { get; set; } = [];
    public ICollection<TeamTrainer> Trainers { get; set; } = [];
    public ICollection<EventTeamEnrollment> EventEnrollments { get; set; } = [];
}
