namespace BookingHub.Api.Models;

public class EventSeries : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Reguła cykliczności, np. "WEEKLY;BYDAY=MO" (RRULE) lub własny format tekstowy.
    /// Używana przez API do generowania kolejnych wystąpień (Event).
    /// </summary>
    public string? RecurrenceRule { get; set; }

    /// <summary>Domyślna grupa zajęciowa — może być nadpisana per Event.</summary>
    public Guid? DefaultGroupId { get; set; }

    /// <summary>Domyślna lokalizacja — może być nadpisana per Event.</summary>
    public Guid? DefaultLocationId { get; set; }

    /// <summary>Domyślny kolor serii w kalendarzu — może być nadpisany per Event.</summary>
    public string? DefaultColor { get; set; }

    /// <summary>Domyślny typ zajęć serii — może być nadpisany per Event.</summary>
    public EventType DefaultEventType { get; set; } = EventType.GroupTraining;

    public bool IsActive { get; set; } = true;
    public Guid? CreatedByPersonId { get; set; }

    public Organization Organization { get; set; } = null!;
    public Group? DefaultGroup { get; set; }
    public Location? DefaultLocation { get; set; }
    public Person? CreatedBy { get; set; }
    public ICollection<Event> Events { get; set; } = [];
}
