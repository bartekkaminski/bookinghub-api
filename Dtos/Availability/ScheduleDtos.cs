namespace BookingHub.Api.Dtos.Availability;

/// <summary>
/// Scalony grafik członka na konkretny dzień — wolne sloty dostępności + zajęcia (Busy).
/// Zajęcia są zawsze widoczne (także poza slotami dostępności).
/// Obszary bez slotu i bez zajęć nie trafiają do odpowiedzi (puste tło osi czasu).
/// </summary>
public sealed class MemberScheduleResponse
{
    public DateOnly Date { get; set; }
    public IReadOnlyList<ScheduleBlock> Blocks { get; set; } = [];
}

public sealed class ScheduleBlock
{
    public TimeOnly TimeFrom { get; set; }
    public TimeOnly TimeTo   { get; set; }
    public ScheduleBlockType Type { get; set; }
    /// <summary>
    /// Id slotu dostępności (MemberAvailability) — Guid.Empty dla zajęć poza slotem.
    /// </summary>
    public Guid SlotId { get; set; }
    /// <summary>Null gdy Type = Available.</summary>
    public ScheduleEventInfo? Event { get; set; }
}

/// <summary>
/// Available = wolny slot dostępności (bez nakładających się zajęć).
/// Busy      = zajęcia (zapis uczestnika lub przypisanie jako trener) — zawsze w grafiku.
/// </summary>
public enum ScheduleBlockType { Available, Busy }

public sealed class ScheduleEventInfo
{
    public Guid    EventId   { get; set; }
    public string  Title     { get; set; } = string.Empty;
    /// <summary>Event.Color ?? Group.Color. Null gdy brak koloru.</summary>
    public string? Color     { get; set; }
    public string  EventType { get; set; } = string.Empty;
}
