using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis aktualnie zalogowanego użytkownika.
/// Dane są odczytywane z HttpContext (JWT claims + encja User z bazy załadowana przez middleware).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Claim 'sub' z JWT — unikalny ID u dostawcy auth.</summary>
    string? ExternalId { get; }

    /// <summary>Id encji User w naszej bazie. Null jeśli użytkownik nie istnieje jeszcze w DB.</summary>
    Guid? UserId { get; }

    /// <summary>Id encji Person powiązanej z tym kontem. Null przed provisioningiem.</summary>
    Guid? PersonId { get; }

    /// <summary>Czy użytkownik jest uwierzytelniony i aktywny.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Encja User załadowana przez middleware (null jeśli brak).</summary>
    User? CurrentUser { get; }

    /// <summary>
    /// Pobiera OrganizationMember aktualnego użytkownika w podanej organizacji.
    /// Zwraca null jeśli użytkownik nie jest członkiem.
    /// </summary>
    Task<OrganizationMember?> GetMemberAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Sprawdza, czy aktualny użytkownik ma daną rolę w organizacji.</summary>
    Task<bool> HasRoleAsync(Guid organizationId, MemberRole role, CancellationToken ct = default);

    /// <summary>Sprawdza, czy aktualny użytkownik jest aktywnym Adminem w organizacji.</summary>
    Task<bool> IsAdminAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Sprawdza, czy aktualny użytkownik jest Managerem w organizacji.</summary>
    Task<bool> IsManagerAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Sprawdza, czy aktualny użytkownik jest Trenerem w organizacji.</summary>
    Task<bool> IsTrainerAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Sprawdza, czy aktualny użytkownik jest Adminem lub Managerem w organizacji.</summary>
    Task<bool> IsAdminOrManagerAsync(Guid organizationId, CancellationToken ct = default);
}
