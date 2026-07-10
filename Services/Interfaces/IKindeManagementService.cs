namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Klient Kinde Management API — zarządzanie kontami przez backend (M2M).
/// Używany gdy admin tworzy konto dla nowego użytkownika.
/// </summary>
public interface IKindeManagementService
{
    /// <summary>
    /// Tworzy nowego użytkownika w Kinde. Zwraca ExternalId (Kinde user_id).
    /// Rzuca ServiceException(EmailAlreadyTaken) jeśli e-mail istnieje.
    /// Rzuca ServiceException(KindeApiError) przy błędzie API.
    /// </summary>
    Task<string> CreateUserInKindeAsync(string firstName, string lastName, string email, CancellationToken ct = default);

    /// <summary>
    /// Zawiesza konto użytkownika w Kinde (blokada logowania po stronie dostawcy auth).
    /// Best-effort — błąd API jest logowany, ale nie blokuje lokalnej operacji.
    /// </summary>
    Task SuspendUserAsync(string externalId, CancellationToken ct = default);

    /// <summary>
    /// Przywraca zawieszone konto użytkownika w Kinde.
    /// </summary>
    Task UnsuspendUserAsync(string externalId, CancellationToken ct = default);
}
