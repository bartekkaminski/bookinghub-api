using System.ComponentModel.DataAnnotations;

namespace BookingHub.Api.Dtos.Person;

/// <summary>Skrócony profil osoby — do list i embeddowanych referencji.</summary>
public sealed class PersonSummaryResponse
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName => FirstName is not null || LastName is not null
        ? $"{FirstName} {LastName}".Trim()
        : null;
    public string? PhotoUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    /// <summary>Czy osoba ma przypisane konto logowania.</summary>
    public bool HasAccount { get; set; }
}

/// <summary>Pełny profil osoby — widok szczegółowy.</summary>
public sealed class PersonDetailResponse
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName => FirstName is not null || LastName is not null
        ? $"{FirstName} {LastName}".Trim()
        : null;
    public string? PhotoUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool HasAccount { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public IReadOnlyList<PersonMembershipInfo> Memberships { get; set; } = [];
    public IReadOnlyList<PersonSummaryResponse> Children { get; set; } = [];
    public IReadOnlyList<PersonSummaryResponse> Parents { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class PersonMembershipInfo
{
    public Guid MemberId { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool IsActive { get; set; }
}

/// <summary>
/// Dane do tworzenia profilu osoby (bez konta logowania — np. dziecko).
/// Konto logowania dodawane jest osobno przez admina lub przy pierwszym logowaniu.
/// </summary>
public sealed class CreatePersonRequest
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(500)]
    [Url]
    public string? PhotoUrl { get; set; }
}

/// <summary>Dane do aktualizacji profilu osoby.</summary>
public sealed class UpdatePersonRequest
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(500)]
    [Url]
    public string? PhotoUrl { get; set; }
}

/// <summary>Żądanie powiązania rodzic–dziecko.</summary>
public sealed class AddParentChildRequest
{
    [Required]
    public Guid ParentPersonId { get; set; }

    [Required]
    public Guid ChildPersonId { get; set; }
}
