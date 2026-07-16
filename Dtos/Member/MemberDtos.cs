using System.ComponentModel.DataAnnotations;
using BookingHub.Api.Models;

namespace BookingHub.Api.Dtos.Member;

/// <summary>Skrócone dane członka organizacji — do list.</summary>
public sealed class MemberSummaryResponse
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>DisplayName jeśli ustawiony, inaczej FirstName + LastName z profilu.</summary>
    public string DisplayName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Color { get; set; }
    public int? Priority { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<MemberRole> Roles { get; set; } = [];

    /// <summary>True gdy osoba ma powiązane konto logowania (Person.UserId != null).</summary>
    public bool HasAccount { get; set; }

    /// <summary>Rangi przypisane przez administratora, jedna per dyscyplina. Pusta lista = brak rang.</summary>
    public IReadOnlyList<MemberRankInfo> Ranks { get; set; } = [];
}

/// <summary>Pełne dane członka organizacji — widok szczegółowy.</summary>
public sealed class MemberDetailResponse
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName => FirstName is not null || LastName is not null
        ? $"{FirstName} {LastName}".Trim()
        : null;
    public string? DisplayName { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Color { get; set; }
    public int? Priority { get; set; }
    public string? PlayerNumber { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public IReadOnlyList<MemberRole> Roles { get; set; } = [];
    public IReadOnlyList<MemberGroupInfo> Groups { get; set; } = [];
    public IReadOnlyList<MemberTeamInfo> Teams { get; set; } = [];
    public IReadOnlyList<MemberTrainerInfo> AssignedTrainers { get; set; } = [];

    /// <summary>Rangi przypisane przez administratora, jedna per dyscyplina. Pusta lista = brak rang.</summary>
    public IReadOnlyList<MemberRankInfo> Ranks { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class MemberGroupInfo
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public sealed class MemberTeamInfo
{
    public Guid TeamId { get; set; }
    public string? TeamName { get; set; }
    public int? Priority { get; set; }
}

public sealed class MemberTrainerInfo
{
    public Guid TrainerMemberId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Color { get; set; }
}

/// <summary>Informacje o randze przypisanej do członka w ramach jednej dyscypliny.</summary>
public sealed class MemberRankInfo
{
    public Guid DisciplineId { get; init; }
    public string DisciplineName { get; init; } = string.Empty;
    public Guid RankId { get; init; }
    public string RankName { get; init; } = string.Empty;
    public string? RankColor { get; init; }
}

/// <summary>Żądanie ustawienia rangi członka. RankId = null usuwa rangę.</summary>
public sealed class SetMemberRankRequest
{
    public Guid? RankId { get; set; }
}

/// <summary>
/// Dane do dodania osoby jako członka organizacji (przez admina).
/// Osoba (Person) musi już istnieć w bazie.
/// </summary>
public sealed class AddMemberRequest
{
    [Required]
    public Guid PersonId { get; set; }

    /// <summary>Co najmniej jedna rola jest wymagana.</summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<MemberRole> Roles { get; set; } = [];

    [StringLength(100)]
    public string? DisplayName { get; set; }

    /// <summary>Kolor hex, np. "#3B82F6". Null = domyślny szary.</summary>
    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Kolor musi być w formacie hex #RRGGBB.")]
    public string? Color { get; set; }

    /// <summary>Priorytet uczestnika. Null = brak priorytetu.</summary>
    [Range(1, int.MaxValue)]
    public int? Priority { get; set; }
}

/// <summary>Dane do aktualizacji danych per-org członka (DisplayName, Color, Priority, Photo) oraz danych osobowych (FirstName, LastName, DateOfBirth).</summary>
public sealed class UpdateMemberRequest
{
    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(100)]
    public string? DisplayName { get; set; }

    [StringLength(500)]
    [Url]
    public string? PhotoUrl { get; set; }

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Kolor musi być w formacie hex #RRGGBB.")]
    public string? Color { get; set; }

    [Range(1, int.MaxValue)]
    public int? Priority { get; set; }

    [StringLength(50)]
    public string? PlayerNumber { get; set; }
}

/// <summary>Żądanie dodania roli do członkostwa.</summary>
public sealed class AddMemberRoleRequest
{
    [Required]
    public MemberRole Role { get; set; }
}

/// <summary>Żądanie ustawienia aktywności członkostwa.</summary>
public sealed class SetMemberActiveRequest
{
    public bool IsActive { get; set; }
}

/// <summary>
/// Żądanie przypisania konta logowania do istniejącego profilu bez konta.
/// Tworzy konto Kinde + User i linkuje z istniejącym Person.
/// </summary>
public sealed class AttachAccountRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}

/// <summary>Żądanie przypisania stałego trenera do uczestnika.</summary>
public sealed class AssignTrainerToParticipantRequest
{
    [Required]
    public Guid TrainerMemberId { get; set; }
}

/// <summary>
/// Wynik wyszukiwania osoby po kodzie profilu — zwracany przez GET /members/find-by-code.
/// Celowo nie zawiera adresu e-mail ani innych danych osobowych poza imieniem i nazwiskiem.
/// </summary>
public sealed class MemberLookupResponse
{
    public Guid PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    /// <summary>True gdy osoba jest już aktywnym członkiem tej organizacji.</summary>
    public bool IsAlreadyMember { get; set; }
}

/// <summary>
/// Dane do tworzenia pełnego użytkownika przez admina:
/// zakłada konto Kinde + Person + OrganizationMember w jednym kroku.
/// </summary>
public sealed class CreateMemberWithAccountRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyList<MemberRole> Roles { get; set; } = [];

    [StringLength(100)]
    public string? DisplayName { get; set; }

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public string? Color { get; set; }

    [Range(1, int.MaxValue)]
    public int? Priority { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(50)]
    public string? PlayerNumber { get; set; }
}

/// <summary>Tworzy profil Person (bez konta Kinde) i dodaje go jako członka organizacji.</summary>
public sealed class CreateMemberProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyList<MemberRole> Roles { get; set; } = [];

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(100)]
    public string? DisplayName { get; set; }

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")]
    public string? Color { get; set; }

    [Range(1, int.MaxValue)]
    public int? Priority { get; set; }

    [StringLength(50)]
    public string? PlayerNumber { get; set; }
}
