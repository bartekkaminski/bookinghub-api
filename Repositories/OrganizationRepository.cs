using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium organizacji (placówek).
/// </summary>
public sealed class OrganizationRepository : BaseRepository<Organization>, IOrganizationRepository
{
    public OrganizationRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Organization?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(o => o.Members)
                .ThenInclude(m => m.Person)
            .Include(o => o.Members)
                .ThenInclude(m => m.Roles)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<Organization>> GetPagedAsync(OrganizationFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        // Wymuszenie filtrowania po PersonId — użytkownik widzi tylko swoje organizacje.
        if (filter.PersonId.HasValue)
            query = query.Where(o => o.Members.Any(m => m.PersonId == filter.PersonId.Value && !m.IsDeleted));

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLowerInvariant();
            query = query.Where(o => o.Name.ToLower().Contains(search));
        }

        query = ApplySorting(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Organization>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<bool> IsNameTakenAsync(string name, Guid? excludeOrganizationId = null, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(
            o => o.Name == name && (excludeOrganizationId == null || o.Id != excludeOrganizationId),
            cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Organization>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(o => o.Members.Any(m => m.PersonId == personId))
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<Organization?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => GetWithMembersAsync(id, cancellationToken);

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IQueryable<Organization> ApplySorting(IQueryable<Organization> query, OrganizationFilterParams filter)
        => filter.SortBy?.ToLowerInvariant() switch
        {
            "name"      => filter.SortDescending ? query.OrderByDescending(o => o.Name)      : query.OrderBy(o => o.Name),
            "createdat" => filter.SortDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt),
            _           => query.OrderBy(o => o.Name)
        };
}
