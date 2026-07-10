using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium profili osób (Person).
/// </summary>
public interface IPersonRepository : IBaseRepository<Person>
{
    /// <summary>
    /// Pobiera profil osoby po powiązanym UserId.
    /// Zwraca null jeśli osoba nie ma konta lub nie istnieje.
    /// </summary>
    Task<Person?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera profil osoby wraz z kontem logowania (User).
    /// </summary>
    Task<Person?> GetWithUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera profil osoby wraz z wszystkimi jej członkostwami w organizacjach.
    /// </summary>
    Task<Person?> GetWithMembershipsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera profil osoby z pełnymi danymi — User, Memberships (z rolami), relacje rodzic–dziecko.
    /// </summary>
    Task<Person?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę profili osób z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<Person>> GetPagedAsync(PersonFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera dzieci powiązane z rodzicem.
    /// </summary>
    Task<IReadOnlyList<Person>> GetChildrenAsync(Guid parentPersonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera rodziców/opiekunów powiązanych z dzieckiem.
    /// </summary>
    Task<IReadOnlyList<Person>> GetParentsAsync(Guid childPersonId, CancellationToken cancellationToken = default);
}
