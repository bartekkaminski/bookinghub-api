using BookingHub.Api.Dtos.Person;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania profilami osób (Person).
/// Person istnieje niezależnie od konta logowania — dziecko może nie mieć konta.
/// </summary>
public interface IPersonService
{
    /// <summary>Pobiera stronicowaną listę profili osób (admin).</summary>
    Task<PagedResult<PersonSummaryResponse>> GetPagedAsync(PersonFilterParams filter, CancellationToken ct = default);

    /// <summary>Pobiera szczegółowy profil osoby wraz z członkostwami, dziećmi i rodzicami.</summary>
    Task<PersonDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Pobiera profil osoby powiązanej z danym kontem User.</summary>
    Task<PersonDetailResponse?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Tworzy nowy profil osoby (bez konta logowania).
    /// Używane przez admina do wczytania uczestnika bez e-maila (np. dziecko).
    /// </summary>
    Task<PersonDetailResponse> CreateAsync(CreatePersonRequest request, CancellationToken ct = default);

    /// <summary>
    /// Tworzy profil osoby i przypisuje go do istniejącego konta User.
    /// Używane podczas ProvisionAsync gdy User jeszcze nie ma Person.
    /// </summary>
    Task<PersonDetailResponse> CreateForUserAsync(Guid userId, CreatePersonRequest request, CancellationToken ct = default);

    /// <summary>Aktualizuje dane profilu osoby.</summary>
    Task<PersonDetailResponse> UpdateAsync(Guid id, UpdatePersonRequest request, CancellationToken ct = default);

    /// <summary>Usuwa profil osoby (soft delete). Tylko gdy brak aktywnych członkostw.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Tworzy powiązanie rodzic–dziecko.</summary>
    Task AddParentChildRelationAsync(Guid parentPersonId, Guid childPersonId, CancellationToken ct = default);

    /// <summary>Usuwa powiązanie rodzic–dziecko.</summary>
    Task RemoveParentChildRelationAsync(Guid parentPersonId, Guid childPersonId, CancellationToken ct = default);

    /// <summary>Pobiera wszystkie dzieci (osoby) danej osoby.</summary>
    Task<IReadOnlyList<PersonSummaryResponse>> GetChildrenAsync(Guid personId, CancellationToken ct = default);

    /// <summary>Pobiera wszystkich rodziców (osoby) danej osoby.</summary>
    Task<IReadOnlyList<PersonSummaryResponse>> GetParentsAsync(Guid personId, CancellationToken ct = default);
}
