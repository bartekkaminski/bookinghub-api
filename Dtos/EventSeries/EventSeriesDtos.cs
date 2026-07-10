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
