using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium tożsamości logowania (User).
/// </summary>
public interface IUserRepository : IBaseRepository<User>
{
    /// <summary>
    /// Pobiera użytkownika po identyfikatorze zewnętrznego dostawcy auth (claim: sub).
    /// Zwraca null jeśli nie istnieje.
    /// </summary>
    Task<User?> GetByExternalIdAsync(string externalId, string authProvider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera użytkownika po adresie e-mail (case-insensitive).
    /// Zwraca null jeśli nie istnieje.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera użytkownika wraz z powiązanym profilem osoby (Person).
    /// </summary>
    Task<User?> GetWithPersonAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę użytkowników z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<User>> GetPagedAsync(UserFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy adres e-mail jest już zajęty przez innego użytkownika.
    /// </summary>
    Task<bool> IsEmailTakenAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy para (externalId, authProvider) jest już zarejestrowana.
    /// </summary>
    Task<bool> IsExternalIdTakenAsync(string externalId, string authProvider, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ustawia flagę IsActive dla użytkownika. Zwraca false jeśli użytkownik nie istnieje.
    /// </summary>
    Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera użytkownika po ExternalId ignorując filtr soft-delete.
    /// Używane przez middleware auth — musi działać nawet gdy konto jest „usunięte".
    /// </summary>
    Task<User?> GetByExternalIdIgnoreFiltersAsync(string externalId, string authProvider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera użytkownika wraz z profilem (Person) po unikalnym kodzie profilu.
    /// Zwraca null jeśli nie istnieje lub kod jest pusty.
    /// </summary>
    Task<User?> GetByProfileCodeAsync(string profileCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy podany kod profilu jest już zajęty przez innego aktywnego użytkownika.
    /// Używane do wykrywania kolizji przed zapisem nowego kodu.
    /// </summary>
    Task<bool> ProfileCodeExistsAsync(string profileCode, CancellationToken cancellationToken = default);
}
