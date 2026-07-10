namespace BookingHub.Api.Models;

public class Event : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Null = wydarzenie jednorazowe. FK = część serii cyklicznej.</summary>
    public Guid? EventSeriesId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    /// <summary>Nadpisuje DefaultLocationId z EventSeries. Null = brak przypisanej lokalizacji.</summary>
    public Guid? LocationId { get; set; }

    /// <summary>Nadpisuje DefaultGroupId z EventSeries. Null = wydarzenie bez grupy (otwarte).</summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// Typ zajęć. Nadpisuje EventSeries.DefaultEventType.
    /// Wpływa na logikę kosztów: IndividualSession = stawka trenera ÷ liczba uczestników,
    /// Camp = UnitCost per uczestnik, GroupTraining = stawka miesięczna grupy.
    /// </summary>
    public EventType EventType { get; set; } = EventType.GroupTraining;

    public EventStatus Status { get; set; } = EventStatus.Scheduled;

    /// <summary>
    /// Jednorazowy koszt per uczestnik — używany gdy EventType=Camp lub inne płatne wydarzenie.
    /// Null = brak stałej ceny (koszt liczony z GroupCostRate lub TrainerSessionRate).
    /// </summary>
    public decimal? UnitCost { get; set; }

    /// <summary>Waluta kosztu jednorazowego, np. "PLN". Null gdy UnitCost jest null.</summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Kolor zajęć w kalendarzu. Null = dziedziczony z EventSeries.DefaultColor
    /// lub z Group.Color (w tej kolejności).
    /// </summary>
    public string? Color { get; set; }

    public Guid? CreatedByPersonId { get; set; }

    public Organization Organization { get; set; } = null!;
    public EventSeries? EventSeries { get; set; }
    public Location? Location { get; set; }
    public Group? Group { get; set; }
    public Person? CreatedBy { get; set; }
    public ICollection<EventTrainer> Trainers { get; set; } = [];
    public ICollection<EventEnrollment> Enrollments { get; set; } = [];
    public ICollection<EventTeamEnrollment> TeamEnrollments { get; set; } = [];
    public ICollection<Message> RelatedMessages { get; set; } = [];
}
