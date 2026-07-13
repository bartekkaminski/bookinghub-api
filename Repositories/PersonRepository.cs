using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium profili osób (Person).
/// </summary>
public sealed class PersonRepository : BaseRepository<Person>, IPersonRepository
{
    public PersonRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Person?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    /// <inheritdoc/>
    public async Task<Person?> GetWithUserAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<Person?> GetWithMembershipsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(p => p.Memberships)
                .ThenInclude(m => m.Organization)
            .Include(p => p.Memberships)
                .ThenInclude(m => m.Roles)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<Person?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Memberships)
                .ThenInclude(m => m.Organization)
            .Include(p => p.Memberships)
                .ThenInclude(m => m.Roles)
            .Include(p => p.ChildRelations)
                .ThenInclude(r => r.Child)
            .Include(p => p.ParentRelations)
                .ThenInclude(r => r.Parent)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<Person>> GetPagedAsync(PersonFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Include(p => p.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLowerInvariant();
            query = query.Where(p =>
                (p.FirstName != null && p.FirstName.ToLower().Contains(search)) ||
                (p.LastName  != null && p.LastName.ToLower().Contains(search)));
        }

        if (filter.HasAccount.HasValue)
        {
            query = filter.HasAccount.Value
                ? query.Where(p => p.UserId != null)
                : query.Where(p => p.UserId == null);
        }

        query = ApplySorting(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Person>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Person>> GetChildrenAsync(Guid parentPersonId, CancellationToken cancellationToken = default)
        => await _context.Set<ParentChildRelation>()
            .AsNoTracking()
            .Where(r => r.ParentPersonId == parentPersonId)
            .Include(r => r.Child)
            .Select(r => r.Child)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Person>> GetParentsAsync(Guid childPersonId, CancellationToken cancellationToken = default)
        => await _context.Set<ParentChildRelation>()
            .AsNoTracking()
            .Where(r => r.ChildPersonId == childPersonId)
            .Include(r => r.Parent)
            .Select(r => r.Parent)
            .ToListAsync(cancellationToken);

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IQueryable<Person> ApplySorting(IQueryable<Person> query, PersonFilterParams filter)
        => filter.SortBy?.ToLowerInvariant() switch
        {
            "firstname"   => filter.SortDescending ? query.OrderByDescending(p => p.FirstName)   : query.OrderBy(p => p.FirstName),
            "lastname"    => filter.SortDescending ? query.OrderByDescending(p => p.LastName)    : query.OrderBy(p => p.LastName),
            "dateofbirth" => filter.SortDescending ? query.OrderByDescending(p => p.DateOfBirth) : query.OrderBy(p => p.DateOfBirth),
            "createdat"   => filter.SortDescending ? query.OrderByDescending(p => p.CreatedAt)   : query.OrderBy(p => p.CreatedAt),
            _             => query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
        };
}
