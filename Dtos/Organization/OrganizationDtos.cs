using System.ComponentModel.DataAnnotations;

namespace BookingHub.Api.Dtos.Organization;

/// <summary>Skrócone dane organizacji — do list.</summary>
public sealed class OrganizationSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MembersCount { get; set; }
}

/// <summary>Pełne dane organizacji — widok szczegółowy.</summary>
public sealed class OrganizationDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MembersCount { get; set; }
    public int ActiveGroupsCount { get; set; }
    public int ActiveTeamsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Dane do tworzenia organizacji.</summary>
public sealed class CreateOrganizationRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }
}

/// <summary>Informacja o limitach tworzenia organizacji dla zalogowanego użytkownika.</summary>
public sealed class OrganizationCreationLimitsResponse
{
    /// <summary>Maksymalna dozwolona liczba tworzonych organizacji. 0 = brak limitu.</summary>
    public int MaxOrganizationsPerCreator { get; set; }

    /// <summary>Ile organizacji użytkownik już utworzył.</summary>
    public int CreatedByMeCount { get; set; }

    /// <summary>Czy użytkownik może jeszcze tworzyć organizacje.</summary>
    public bool CanCreate { get; set; }
}

/// <summary>Dane do aktualizacji organizacji.</summary>
public sealed class UpdateOrganizationRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }
}
