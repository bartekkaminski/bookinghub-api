using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Generyczny interfejs repozytorium dla encji dziedziczących z <see cref="BaseEntity"/>.
/// Definiuje podstawowe operacje CRUD dostępne dla każdej encji.
/// </summary>
/// <typeparam name="T">Typ encji dziedziczącej z BaseEntity.</typeparam>
public interface IBaseRepository<T> where T : BaseEntity
{
    /// <summary>Pobiera encję po identyfikatorze. Zwraca null jeśli nie istnieje lub jest soft-deleted.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie encje (bez paginacji).
    /// Używać ostrożnie — dla dużych zbiorów stosować GetPagedAsync.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Dodaje nową encję do kontekstu i zapisuje zmiany.</summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Dodaje wiele encji w jednej transakcji (bulk insert).</summary>
    Task<IReadOnlyList<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>Aktualizuje istniejącą encję i zapisuje zmiany.</summary>
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa encję (soft delete przez AppDbContext — ustawia IsDeleted=true, DeletedAt=now).
    /// Zwraca false jeśli encja nie istnieje.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Sprawdza, czy encja o podanym identyfikatorze istnieje (nie jest soft-deleted).</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
