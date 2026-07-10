using System.ComponentModel.DataAnnotations;

namespace BookingHub.Api.Dtos.Auth;

/// <summary>
/// Dane do auto-provisioningu przy pierwszym logowaniu przez Kinde.
/// Wypełniane z claims JWT tokenu.
/// </summary>
public sealed class ProvisionUserRequest
{
    /// <summary>Claim 'sub' z JWT — unikalny ID użytkownika u dostawcy auth.</summary>
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>Nazwa dostawcy auth, np. "kinde".</summary>
    [Required]
    public string AuthProvider { get; set; } = "kinde";

    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

/// <summary>
/// Odpowiedź po provisioningu — dane zalogowanego użytkownika + jego profil.
/// </summary>
public sealed class AuthMeResponse
{
    public Guid UserId { get; set; }
    public Guid PersonId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName => FirstName is not null || LastName is not null
        ? $"{FirstName} {LastName}".Trim()
        : null;
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
    public string PreferredLanguage { get; set; } = "pl";

    /// <summary>Organizacje i role — potrzebne do routingu frontendu.</summary>
    public IReadOnlyList<AuthMembershipInfo> Memberships { get; set; } = [];
}

public sealed class AuthMembershipInfo
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}

/// <summary>
/// Żądanie zmiany preferowanego języka UI.
/// </summary>
public sealed class SetPreferredLanguageRequest
{
    [Required]
    [RegularExpression("^(pl|en)$", ErrorMessage = "Dozwolone wartości: 'pl', 'en'.")]
    public string Language { get; set; } = "pl";
}
