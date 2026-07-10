using BookingHub.Api.Dtos.Auth;
using BookingHub.Api.Dtos.User;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania kontami logowania (User + powiązanie z Kinde).
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Auto-provisioning: tworzy lub synchronizuje rekord User przy pierwszym logowaniu.
    /// Wywoływane przez POST /api/auth/me po każdym logowaniu przez Kinde.
    /// </summary>
    Task<AuthMeResponse> ProvisionAsync(ProvisionUserRequest request, CancellationToken ct = default);

    /// <summary>Pobiera dane konta po jego wewnętrznym Id.</summary>
    Task<UserDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Pobiera dane konta po ExternalId (Kinde user_id).</summary>
    Task<UserDetailResponse?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);

    /// <summary>
    /// Pobiera profil zalogowanego użytkownika (AuthMeResponse z członkostwami) — read-only.
    /// Używane przez GET /api/auth/me.
    /// </summary>
    Task<AuthMeResponse?> GetMeAsync(string externalId, CancellationToken ct = default);

    /// <summary>Zmienia stan aktywności konta (globalny toggle, synchronizowany z Kinde).</summary>
    Task<UserDetailResponse> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);

    /// <summary>Ustawia preferowany język UI dla zalogowanego użytkownika.</summary>
    Task SetPreferredLanguageAsync(string externalId, string language, CancellationToken ct = default);

    /// <summary>
    /// Usuwa konto (soft delete). Nie usuwa profilu Person ani członkostw.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
