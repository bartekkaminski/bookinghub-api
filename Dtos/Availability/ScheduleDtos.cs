namespace BookingHub.Api.Dtos.Availability;

/// <summary>
/// Scalony grafik członka na konkretny dzień — sloty dostępności + zajęcia → bloki Available/Busy.
/// Obszary poza slotami dostępności (Unavailable) nie trafiają do odpowiedzi.
/// Frontend renderuje je jako puste tło osi czasu.
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
    /// Id slotu dostępności (MemberAvailability) z którego pochodzi ten blok.
    /// Dzięki temu frontend może otworzyć edycję właściwego slotu po kliknięciu bloku.
    /// </summary>
    public Guid SlotId { get; set; }
    /// <summary>Null gdy Type = Available.</summary>
    public ScheduleEventInfo? Event { get; set; }
}

/// <summary>
/// Available = slot dostępności wolny od zajęć (można zarezerwować).
/// Busy      = slot dostępności pokryty przez zajęcia.
/// Unavailable NIE wchodzi do DTO — frontend renderuje je jako puste tło.
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
