using System.ComponentModel.DataAnnotations;

namespace BookingHub.Api.Dtos.Rank;

/// <summary>Skrócone dane rangi — do list.</summary>
public sealed class RankSummaryResponse
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string Name { get; init; } = string.Empty;

    /// <summary>Kolor HEX rangi, np. "#F59E0B". Null = brak koloru.</summary>
    public string? Color { get; init; }

    /// <summary>Liczba aktywnych członków posiadających tę rangę.</summary>
    public int MemberCount { get; init; }
}

/// <summary>Pełne dane rangi — widok szczegółowy.</summary>
public sealed class RankDetailResponse
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Color { get; init; }
    public int MemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>Dane do tworzenia nowej rangi.</summary>
public sealed class CreateRankRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Kolor musi być w formacie hex #RRGGBB.")]
    public string? Color { get; set; }
}

/// <summary>Dane do aktualizacji rangi.</summary>
public sealed class UpdateRankRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Kolor musi być w formacie hex #RRGGBB.")]
    public string? Color { get; set; }
}
