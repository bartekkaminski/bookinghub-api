using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium relacji rodzic–dziecko (ParentChildRelation).
/// </summary>
public sealed class ParentChildRelationRepository : BaseRepository<ParentChildRelation>, IParentChildRelationRepository
{
    public ParentChildRelationRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<ParentChildRelation?> GetByParentAndChildAsync(Guid parentPersonId, Guid childPersonId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ParentPersonId == parentPersonId && r.ChildPersonId == childPersonId, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<ParentChildRelation>> GetPagedAsync(ParentChildRelationFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(r => r.Parent)
            .Include(r => r.Child)
            .AsQueryable();

        if (filter.ParentPersonId.HasValue)
            query = query.Where(r => r.ParentPersonId == filter.ParentPersonId.Value);

        if (filter.ChildPersonId.HasValue)
            query = query.Where(r => r.ChildPersonId == filter.ChildPersonId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Child.LastName)
            .ThenBy(r => r.Child.FirstName)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ParentChildRelation>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ParentChildRelation>> GetChildrenByParentAsync(Guid parentPersonId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(r => r.Child)
            .Where(r => r.ParentPersonId == parentPersonId)
            .OrderBy(r => r.Child.LastName)
            .ThenBy(r => r.Child.FirstName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ParentChildRelation>> GetParentsByChildAsync(Guid childPersonId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(r => r.Parent)
            .Where(r => r.ChildPersonId == childPersonId)
            .OrderBy(r => r.Parent.LastName)
            .ThenBy(r => r.Parent.FirstName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> RelationExistsAsync(Guid parentPersonId, Guid childPersonId, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(r => r.ParentPersonId == parentPersonId && r.ChildPersonId == childPersonId, cancellationToken);
}
