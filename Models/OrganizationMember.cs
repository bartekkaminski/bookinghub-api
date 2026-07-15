namespace BookingHub.Api.Models;

public class OrganizationMember : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid PersonId { get; set; }

    /// <summary>
    /// Pseudonim/wyświetlana nazwa per-organizacja.
    /// Jeśli null — używane są Person.FirstName + Person.LastName.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Zdjęcie profilowe per-organizacja.
    /// Jeśli null — używane jest Person.PhotoUrl.
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Kolor członka w kalendarzu, np. "#3B82F6". Używany przy zajęciach indywidualnych (trenerzy).
    /// Null = domyślny szary.
    /// </summary>
    public string? Color { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priorytet uczestnika ustawiony przez admina (min 1).
    /// Null = brak priorytetu. Używany przy planowaniu slotów i wyświetlaniu list.
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// Numer zawodnika nadany przez admina (np. "10", "1A").
    /// Null = brak numeru. Pole per-organizacja.
    /// </summary>
    public string? PlayerNumber { get; set; }

    /// <summary>Admin który dodał tę osobę do organizacji. Null = pierwszy admin (dodał sam siebie).</summary>
    public Guid? CreatedByPersonId { get; set; }

    /// <summary>
    /// Ranga przypisana przez administratora. Null = brak rangi.
    /// Ustawiana przez PUT /members/{id}/rank.
    /// </summary>
    public Guid? RankId { get; set; }

    public Organization Organization { get; set; } = null!;
    public Person Person { get; set; } = null!;
    public Person? CreatedBy { get; set; }
    public OrganizationRank? Rank { get; set; }
    public ICollection<OrganizationMemberRole> Roles { get; set; } = [];
    public ICollection<GroupMember> GroupMemberships { get; set; } = [];
    public ICollection<TeamMember> TeamMemberships { get; set; } = [];
    public ICollection<EventTrainer> EventsAsTrainer { get; set; } = [];
    public ICollection<EventEnrollment> EventEnrollments { get; set; } = [];

    /// <summary>Stali trenerzy tego uczestnika</summary>
    public ICollection<ParticipantTrainer> AssignedTrainers { get; set; } = [];

    /// <summary>Uczestnicy których ten członek stale prowadzi (gdy ma rolę Trainer)</summary>
    public ICollection<ParticipantTrainer> AssignedParticipants { get; set; } = [];

    /// <summary>Składy które ten członek stale prowadzi (gdy ma rolę Trainer)</summary>
    public ICollection<TeamTrainer> AssignedTeams { get; set; } = [];

    /// <summary>Sloty dostępności (używane przez trenerów i uczestników)</summary>
    public ICollection<MemberAvailability> Availability { get; set; } = [];

    /// <summary>Historia stawek godzinowych (gdy ma rolę Trainer)</summary>
    public ICollection<TrainerSessionRate> SessionRates { get; set; } = [];
    public ICollection<Message> SentMessages { get; set; } = [];
    public ICollection<MessageRecipient> ReceivedMessages { get; set; } = [];
}
