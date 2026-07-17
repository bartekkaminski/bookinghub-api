using System.ComponentModel.DataAnnotations;
using BookingHub.Api.Models;

namespace BookingHub.Api.Dtos.Event;

/// <summary>Skrócone dane zajęć — do list i widoku kalendarza.</summary>
public sealed class EventSummaryResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public EventType EventType { get; set; }
    public EventStatus Status { get; set; }
    public string? Color { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public Guid? SeriesGroupId { get; set; }
    public int EnrolledCount { get; set; }
    public IReadOnlyList<EventTrainerInfo> Trainers { get; set; } = [];
}

/// <summary>
/// Zoptymalizowana odpowiedź dla widoku kalendarza (mniej danych, szybkie ładowanie).
/// </summary>
public sealed class EventCalendarResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public EventType EventType { get; set; }
    public EventStatus Status { get; set; }
    /// <summary>Kolor zajęć (własny → grupy → szary domyślny).</summary>
    public string Color { get; set; } = "#9CA3AF";
    public string? LocationName { get; set; }
    public string? GroupName { get; set; }
    public IReadOnlyList<string> TrainerNames { get; set; } = [];
    public int EnrolledCount { get; set; }
    public Guid? SeriesGroupId { get; set; }
}

/// <summary>Pełne dane zajęć — widok szczegółowy.</summary>
public sealed class EventDetailResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? SeriesGroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public EventType EventType { get; set; }
    public EventStatus Status { get; set; }
    public string? Color { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Currency { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? LocationAddress { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public IReadOnlyList<EventTrainerInfo> Trainers { get; set; } = [];
    public IReadOnlyList<EventEnrollmentInfo> Enrollments { get; set; } = [];
    public IReadOnlyList<EventTeamEnrollmentInfo> TeamEnrollments { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class EventTrainerInfo
{
    public Guid MemberId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? PhotoUrl { get; set; }
}

public sealed class EventEnrollmentInfo
{
    public Guid EnrollmentId { get; set; }
    public Guid MemberId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasPendingCancellation { get; set; }
}

public sealed class EventTeamEnrollmentInfo
{
    public Guid EnrollmentId { get; set; }
    public Guid TeamId { get; set; }
    public string? TeamName { get; set; }
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<string> MemberNames { get; set; } = [];
}

/// <summary>Dane do tworzenia jednorazowych zajęć.</summary>
public sealed class CreateEventRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public Guid? LocationId { get; set; }
    public Guid? GroupId { get; set; }
    public EventType EventType { get; set; } = EventType.GroupTraining;

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public string? Color { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? UnitCost { get; set; }

    [StringLength(3)]
    public string? Currency { get; set; }

    /// <summary>Uczestnicy do zapisania od razu przy tworzeniu zajęć.</summary>
    public IReadOnlyList<Guid> MemberIds { get; set; } = [];

    /// <summary>Zespoły do zapisania od razu przy tworzeniu zajęć.</summary>
    public IReadOnlyList<Guid> TeamIds { get; set; } = [];
}

/// <summary>Dane do tworzenia cyklu zajęć (wiele Event z tym samym SeriesGroupId).</summary>
public sealed class CreateRecurringEventsRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public EventType EventType { get; set; } = EventType.GroupTraining;
    public Guid? LocationId { get; set; }
    public Guid? GroupId { get; set; }

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public string? Color { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? UnitCost { get; set; }

    [StringLength(3)]
    public string? Currency { get; set; }

    /// <summary>Uczestnicy zapisywani na każde wygenerowane wystąpienie.</summary>
    public IReadOnlyList<Guid> MemberIds { get; set; } = [];

    /// <summary>Zespoły zapisywane na każde wygenerowane wystąpienie.</summary>
    public IReadOnlyList<Guid> TeamIds { get; set; } = [];

    /// <summary>Data początkowa zakresu generowania (włącznie).</summary>
    [Required]
    public DateOnly DateFrom { get; set; }

    /// <summary>Data końcowa zakresu generowania (włącznie).</summary>
    [Required]
    public DateOnly DateTo { get; set; }

    /// <summary>Godzina rozpoczęcia każdych zajęć (np. 18:00).</summary>
    [Required]
    public TimeOnly StartTime { get; set; }

    /// <summary>Godzina zakończenia każdych zajęć (np. 19:30).</summary>
    [Required]
    public TimeOnly EndTime { get; set; }

    /// <summary>Dni tygodnia, w które mają powstawać zajęcia.</summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; set; } = [];
}

/// <summary>Wynik operacji tworzenia cyklu zajęć.</summary>
public sealed class CreateRecurringEventsResponse
{
    /// <summary>Identyfikator grupy cyklu wspólny dla wszystkich utworzonych zajęć.</summary>
    public Guid SeriesGroupId { get; set; }

    /// <summary>Liczba nowo utworzonych zajęć.</summary>
    public int GeneratedCount { get; set; }

    /// <summary>Liczba pominięć — zajęcia o tym samym StartTime w tym cyklu już istniały.</summary>
    public int SkippedCount { get; set; }

    /// <summary>Identyfikatory nowo utworzonych zajęć (w kolejności chronologicznej).</summary>
    public IReadOnlyList<Guid> EventIds { get; set; } = [];
}

/// <summary>Żądanie odwołania wszystkich przyszłych zajęć w cyklu.</summary>
public sealed class CancelFutureInSeriesGroupRequest
{
    /// <summary>Powód odwołania — użyty w automatycznej wiadomości do uczestników.</summary>
    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>Czy automatycznie powiadomić uczestników wiadomością systemową.</summary>
    public bool NotifyParticipants { get; set; } = true;
}

/// <summary>Wynik odwołania przyszłych zajęć w cyklu.</summary>
public sealed class CancelFutureInSeriesGroupResponse
{
    public Guid SeriesGroupId { get; set; }
    public int CancelledCount { get; set; }
}

/// <summary>Dane do aktualizacji zajęć.</summary>
public sealed class UpdateEventRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public Guid? LocationId { get; set; }
    public Guid? GroupId { get; set; }
    public EventType EventType { get; set; } = EventType.GroupTraining;

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public string? Color { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? UnitCost { get; set; }

    [StringLength(3)]
    public string? Currency { get; set; }
}

/// <summary>Żądanie odwołania zajęć (zmiana statusu na Cancelled).</summary>
public sealed class CancelEventRequest
{
    /// <summary>Powód odwołania — zostanie użyty w automatycznej wiadomości do uczestników.</summary>
    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>Czy automatycznie powiadomić uczestników wiadomością systemową.</summary>
    public bool NotifyParticipants { get; set; } = true;
}

/// <summary>Żądanie przypisania trenera do zajęć.</summary>
public sealed class AssignTrainerToEventRequest
{
    [Required]
    public Guid OrganizationMemberId { get; set; }
}

/// <summary>Parametry pobierania kalendarza.</summary>
public sealed class CalendarRequest
{
    [Required]
    public DateTime From { get; set; }

    [Required]
    public DateTime To { get; set; }

    public Guid? GroupId { get; set; }
    public Guid? LocationId { get; set; }
    public EventType? EventType { get; set; }
    public EventStatus? Status { get; set; }
}
