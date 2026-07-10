using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium serii cyklicznych zajęć (EventSeries).
/// </summary>
public sealed class EventSeriesRepository : BaseRepository<EventSeries>, IEventSeriesRepository
{
    public EventSeriesRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<EventSeries?> GetWithEventsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(es => es.Events.OrderBy(e => e.StartTime))
            .FirstOrDefaultAsync(es => es.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<EventSeries>> GetPagedAsync(EventSeriesFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (filter.OrganizationId.HasValue)
            query = query.Where(es => es.OrganizationId == filter.OrganizationId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLowerInvariant();
            query = query.Where(es => es.Title.ToLower().Contains(search));
        }

        if (filter.DefaultGroupId.HasValue)
            query = query.Where(es => es.DefaultGroupId == filter.DefaultGroupId.Value);

        if (filter.DefaultLocationId.HasValue)
            query = query.Where(es => es.DefaultLocationId == filter.DefaultLocationId.Value);

        if (filter.DefaultEventType.HasValue)
            query = query.Where(es => es.DefaultEventType == filter.DefaultEventType.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(es => es.IsActive == filter.IsActive.Value);

        query = ApplySorting(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EventSeries>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EventSeries>> GetByOrganizationAsync(Guid organizationId, bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(es => es.OrganizationId == organizationId);
        if (onlyActive) query = query.Where(es => es.IsActive);
        return await query.OrderBy(es => es.Title).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EventSeries>> GetByDefaultGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(es => es.DefaultGroupId == groupId)
            .OrderBy(es => es.Title)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<EventSeries?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(es => es.DefaultGroup)
            .Include(es => es.DefaultLocation)
            .Include(es => es.Events.OrderBy(e => e.StartTime))
            .FirstOrDefaultAsync(es => es.Id == id, cancellationToken);

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IQueryable<EventSeries> ApplySorting(IQueryable<EventSeries> query, EventSeriesFilterParams filter)
        => filter.SortBy?.ToLowerInvariant() switch
        {
            "title"     => filter.SortDescending ? query.OrderByDescending(es => es.Title)     : query.OrderBy(es => es.Title),
            "createdat" => filter.SortDescending ? query.OrderByDescending(es => es.CreatedAt) : query.OrderBy(es => es.CreatedAt),
            _           => query.OrderBy(es => es.Title)
        };
}
