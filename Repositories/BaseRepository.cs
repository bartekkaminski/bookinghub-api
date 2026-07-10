using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Generyczna implementacja bazowa repozytorium.
/// Obsługuje podstawowe operacje CRUD dla encji dziedziczących z <see cref="BaseEntity"/>.
/// Soft delete jest realizowany przez globalny query filter oraz interceptor w <see cref="AppDbContext"/>.
/// </summary>
/// <typeparam name="T">Typ encji dziedziczącej z BaseEntity.</typeparam>
public abstract class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected BaseRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    /// <inheritdoc/>
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc/>
    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task<IReadOnlyList<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();
        await _dbSet.AddRangeAsync(list, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return list;
    }

    /// <inheritdoc/>
    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        // FindAsync sprawdza change tracker, a jeśli nie ma — ładuje z DB z trackowaniem.
        // Dzięki temu UpdatedAt jest ustawiane przez interceptor audytu na podstawie diff.
        var tracked = await _dbSet.FindAsync([entity.Id], cancellationToken);
        if (tracked is not null)
        {
            _context.Entry(tracked).CurrentValues.SetValues(entity);
        }
        else
        {
            _dbSet.Update(entity);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync([id], cancellationToken);
        if (entity is null) return false;

        // AppDbContext przechwytuje Remove i zamienia na soft delete (IsDeleted=true, DeletedAt=now).
        // Kaskadowy soft delete potomnych encji obsługiwany przez SaveChangesAsync w AppDbContext.
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(e => e.Id == id, cancellationToken);
}
