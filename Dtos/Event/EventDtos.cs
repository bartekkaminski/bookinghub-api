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
    public Guid? EventSeriesId { get; set; }
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
    /// <summary>Kolor zajęć (własny → serii → grupy → szary domyślny).</summary>
    public string Color { get; set; } = "#9CA3AF";
    public string? LocationName { get; set; }
    public string? GroupName { get; set; }
    public IReadOnlyList<string> TrainerNames { get; set; } = [];
    public int EnrolledCount { get; set; }
    public Guid? EventSeriesId { get; set; }
}

/// <summary>Pełne dane zajęć — widok szczegółowy.</summary>
public sealed class EventDetailResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? EventSeriesId { get; set; }
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

/// <summary>Dane do tworzenia zajęć (jednorazowych lub jako część serii).</summary>
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
    public Guid? EventSeriesId { get; set; }
    public EventType EventType { get; set; } = EventType.GroupTraining;

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public string? Color { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? UnitCost { get; set; }

    [StringLength(3)]
    public string? Currency { get; set; }

    /// <summary>Opcjonalnie: trenerzy do przypisania od razu przy tworzeniu.</summary>
    public IReadOnlyList<Guid> TrainerMemberIds { get; set; } = [];
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
