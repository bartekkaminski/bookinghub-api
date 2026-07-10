using System.ComponentModel.DataAnnotations;

namespace BookingHub.Api.Dtos.Location;

/// <summary>Skrócone dane lokalizacji — do list i selectów.</summary>
public sealed class LocationSummaryResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Pełne dane lokalizacji — widok szczegółowy.</summary>
public sealed class LocationDetailResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Dane do tworzenia lokalizacji.</summary>
public sealed class CreateLocationRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
}

/// <summary>Dane do aktualizacji lokalizacji.</summary>
public sealed class UpdateLocationRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
