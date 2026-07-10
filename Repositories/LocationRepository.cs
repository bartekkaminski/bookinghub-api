using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium lokalizacji zajęć (Location).
/// </summary>
public sealed class LocationRepository : BaseRepository<Location>, ILocationRepository
{
    public LocationRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<PagedResult<Location>> GetPagedAsync(LocationFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (filter.OrganizationId.HasValue)
            query = query.Where(l => l.OrganizationId == filter.OrganizationId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLowerInvariant();
            query = query.Where(l =>
                l.Name.ToLower().Contains(search) ||
                (l.Address != null && l.Address.ToLower().Contains(search)));
        }

        if (filter.IsActive.HasValue)
            query = query.Where(l => l.IsActive == filter.IsActive.Value);

        query = ApplySorting(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Location>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Location>> GetByOrganizationAsync(Guid organizationId, bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(l => l.OrganizationId == organizationId);
        if (onlyActive) query = query.Where(l => l.IsActive);
        return await query.OrderBy(l => l.Name).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsNameTakenInOrgAsync(Guid organizationId, string name, Guid? excludeLocationId = null, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(
            l => l.OrganizationId == organizationId
              && l.Name == name
              && (excludeLocationId == null || l.Id != excludeLocationId),
            cancellationToken);

    /// <inheritdoc/>
    public Task<bool> IsNameTakenAsync(Guid organizationId, string name, Guid? excludeLocationId = null, CancellationToken cancellationToken = default)
        => IsNameTakenInOrgAsync(organizationId, name, excludeLocationId, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> HasUpcomingEventsAsync(Guid locationId, CancellationToken cancellationToken = default)
        => await _context.Set<Event>()
            .AnyAsync(e => e.LocationId == locationId && e.StartTime > DateTime.UtcNow && e.Status == EventStatus.Scheduled,
                cancellationToken);

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IQueryable<Location> ApplySorting(IQueryable<Location> query, LocationFilterParams filter)
        => filter.SortBy?.ToLowerInvariant() switch
        {
            "name"      => filter.SortDescending ? query.OrderByDescending(l => l.Name)      : query.OrderBy(l => l.Name),
            "createdat" => filter.SortDescending ? query.OrderByDescending(l => l.CreatedAt) : query.OrderBy(l => l.CreatedAt),
            _           => query.OrderBy(l => l.Name)
        };
}
