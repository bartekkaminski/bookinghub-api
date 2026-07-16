using System.ComponentModel.DataAnnotations;

namespace BookingHub.Api.Dtos.Discipline;

/// <summary>Skrócone dane dyscypliny — do list.</summary>
public sealed class DisciplineSummaryResponse
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string Name { get; init; } = string.Empty;

    /// <summary>Kolor HEX dyscypliny, np. "#F59E0B". Null = brak koloru.</summary>
    public string? Color { get; init; }

    /// <summary>Liczba rang zdefiniowanych w tej dyscyplinie.</summary>
    public int RankCount { get; init; }
}

/// <summary>Pełne dane dyscypliny — widok szczegółowy.</summary>
public sealed class DisciplineDetailResponse
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Color { get; init; }
    public int RankCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>Dane do tworzenia nowej dyscypliny.</summary>
public sealed class CreateDisciplineRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Kolor musi być w formacie hex #RRGGBB.")]
    public string? Color { get; set; }
}

/// <summary>Dane do aktualizacji dyscypliny.</summary>
public sealed class UpdateDisciplineRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Kolor musi być w formacie hex #RRGGBB.")]
    public string? Color { get; set; }
}
