using System.ComponentModel.DataAnnotations;

namespace BookingHub.Api.Dtos.User;

/// <summary>Skrócone dane konta logowania — do list i referencji.</summary>
public sealed class UserSummaryResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Pełne dane konta logowania — widok szczegółowy.</summary>
public sealed class UserDetailResponse
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string AuthProvider { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    /// <summary>Powiązany profil osoby (Person), o ile istnieje.</summary>
    public Guid? PersonId { get; set; }
}

/// <summary>Żądanie zmiany adresu e-mail konta (synchronizowane z Kinde).</summary>
public sealed class UpdateUserEmailRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}

/// <summary>Żądanie ustawienia flagi IsActive (blokada / odblokowanie globalne).</summary>
public sealed class SetUserActiveRequest
{
    public bool IsActive { get; set; }
}
