using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium relacji rodzic–dziecko (ParentChildRelation).
/// Zarządzanie wyłącznie przez Administratora.
/// </summary>
public interface IParentChildRelationRepository : IBaseRepository<ParentChildRelation>
{
    /// <summary>
    /// Pobiera konkretną relację rodzic–dziecko.
    /// Zwraca null jeśli relacja nie istnieje.
    /// </summary>
    Task<ParentChildRelation?> GetByParentAndChildAsync(Guid parentPersonId, Guid childPersonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę relacji z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<ParentChildRelation>> GetPagedAsync(ParentChildRelationFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie dzieci/podopiecznych danego rodzica (z profilem Person).
    /// </summary>
    Task<IReadOnlyList<ParentChildRelation>> GetChildrenByParentAsync(Guid parentPersonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkich rodziców/opiekunów danego dziecka (z profilem Person).
    /// </summary>
    Task<IReadOnlyList<ParentChildRelation>> GetParentsByChildAsync(Guid childPersonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy dana relacja rodzic–dziecko już istnieje.
    /// </summary>
    Task<bool> RelationExistsAsync(Guid parentPersonId, Guid childPersonId, CancellationToken cancellationToken = default);
}
