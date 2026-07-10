using System.ComponentModel.DataAnnotations;

namespace BookingHub.Api.Dtos.Availability;

/// <summary>Dane slotu dostępności trenera lub uczestnika.</summary>
public sealed class AvailabilitySlotResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationMemberId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly TimeFrom { get; set; }
    public TimeOnly TimeTo { get; set; }
    /// <summary>Null = od zawsze.</summary>
    public DateOnly? ValidFrom { get; set; }
    /// <summary>Null = bezterminowo.</summary>
    public DateOnly? ValidTo { get; set; }
}

/// <summary>Dane do dodania slotu dostępności.</summary>
public sealed class AddAvailabilitySlotRequest
{
    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeOnly TimeFrom { get; set; }

    [Required]
    public TimeOnly TimeTo { get; set; }

    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

/// <summary>Dane do aktualizacji slotu dostępności.</summary>
public sealed class UpdateAvailabilitySlotRequest
{
    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeOnly TimeFrom { get; set; }

    [Required]
    public TimeOnly TimeTo { get; set; }

    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

/// <summary>Wynik sprawdzenia dostępności kilku osób w danym terminie.</summary>
public sealed class AvailabilityCheckResponse
{
    public DateTime CheckFrom { get; set; }
    public DateTime CheckTo { get; set; }
    public IReadOnlyList<MemberAvailabilityInfo> Members { get; set; } = [];
}

public sealed class MemberAvailabilityInfo
{
    public Guid MemberId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    /// <summary>Pasujące sloty dostępności, gdy IsAvailable = true.</summary>
    public IReadOnlyList<AvailabilitySlotResponse> MatchingSlots { get; set; } = [];
}
