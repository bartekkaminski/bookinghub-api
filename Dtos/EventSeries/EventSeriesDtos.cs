using System.ComponentModel.DataAnnotations;
using BookingHub.Api.Models;

namespace BookingHub.Api.Dtos.EventSeries;

/// <summary>Skrócone dane serii cyklicznej — do list.</summary>
public sealed class EventSeriesSummaryResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? RecurrenceRule { get; set; }
    public EventType DefaultEventType { get; set; }
    public string? DefaultColor { get; set; }
    public Guid? DefaultGroupId { get; set; }
    public string? DefaultGroupName { get; set; }
    public Guid? DefaultLocationId { get; set; }
    public string? DefaultLocationName { get; set; }
    public bool IsActive { get; set; }
    public int EventsCount { get; set; }
}

/// <summary>Pełne dane serii cyklicznej — widok szczegółowy.</summary>
public sealed class EventSeriesDetailResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RecurrenceRule { get; set; }
    public EventType DefaultEventType { get; set; }
    public string? DefaultColor { get; set; }
    public Guid? DefaultGroupId { get; set; }
    public string? DefaultGroupName { get; set; }
    public Guid? DefaultLocationId { get; set; }
    public string? DefaultLocationName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Dane do tworzenia serii cyklicznej.</summary>
public sealed class CreateEventSeriesRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>Reguła cykliczności w formacie iCal RRULE, np. "FREQ=WEEKLY;BYDAY=TU".</summary>
    [StringLength(500)]
    public string? RecurrenceRule { get; set; }

    public Guid? DefaultGroupId { get; set; }
    public Guid? DefaultLocationId { get; set; }

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public string? DefaultColor { get; set; }

    public EventType DefaultEventType { get; set; } = EventType.GroupTraining;
}

/// <summary>Dane do aktualizacji serii cyklicznej.</summary>
public sealed class UpdateEventSeriesRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? RecurrenceRule { get; set; }

    public Guid? DefaultGroupId { get; set; }
    public Guid? DefaultLocationId { get; set; }

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public string? DefaultColor { get; set; }

    public EventType DefaultEventType { get; set; } = EventType.GroupTraining;
    public bool IsActive { get; set; } = true;
}

/// <summary>Żądanie auto-generowania zajęć z reguły cykliczności serii.</summary>
public sealed class GenerateEventsRequest
{
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

    /// <summary>Nadpisuje DefaultLocationId z serii — null = użyj domyślnej.</summary>
    public Guid? OverrideLocationId { get; set; }

    /// <summary>Nadpisuje DefaultGroupId z serii — null = użyj domyślnej.</summary>
    public Guid? OverrideGroupId { get; set; }

    /// <summary>Nadpisuje DefaultColor z serii — null = użyj domyślnego.</summary>
    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public string? OverrideColor { get; set; }
}

/// <summary>Wynik operacji generowania zajęć z serii.</summary>
public sealed class GenerateEventsResponse
{
    /// <summary>Liczba nowo utworzonych zajęć.</summary>
    public int GeneratedCount { get; set; }

    /// <summary>Liczba pominięć — zajęcia o tym samym StartTime w tej serii już istniały.</summary>
    public int SkippedCount { get; set; }
}
