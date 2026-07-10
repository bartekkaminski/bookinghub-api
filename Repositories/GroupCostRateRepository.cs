using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium historii stawek miesięcznych grup zajęciowych (GroupCostRate).
/// </summary>
public sealed class GroupCostRateRepository : BaseRepository<GroupCostRate>, IGroupCostRateRepository
{
    public GroupCostRateRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<PagedResult<GroupCostRate>> GetPagedAsync(GroupCostRateFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (filter.GroupId.HasValue)
            query = query.Where(r => r.GroupId == filter.GroupId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Currency))
            query = query.Where(r => r.Currency == filter.Currency);

        if (filter.ValidFrom.HasValue)
            query = query.Where(r => r.ValidFrom >= filter.ValidFrom.Value);

        if (filter.ValidTo.HasValue)
            query = query.Where(r => r.ValidTo == null || r.ValidTo <= filter.ValidTo.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.ValidFrom)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<GroupCostRate>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GroupCostRate>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(r => r.GroupId == groupId)
            .OrderByDescending(r => r.ValidFrom)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<GroupCostRate?> GetCurrentByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(r => r.GroupId == groupId && r.ValidTo == null)
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<GroupCostRate?> GetRateOnDateAsync(Guid groupId, DateOnly date, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(r => r.GroupId == groupId
                     && r.ValidFrom <= date
                     && (r.ValidTo == null || r.ValidTo >= date))
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<Dictionary<Guid, GroupCostRate?>> GetRatesOnDateForGroupsAsync(IEnumerable<Guid> groupIds, DateOnly date, CancellationToken cancellationToken = default)
    {
        var ids = groupIds.ToList();
        if (ids.Count == 0) return [];

        var rates = await _dbSet
            .AsNoTracking()
            .Where(r => ids.Contains(r.GroupId)
                     && r.ValidFrom <= date
                     && (r.ValidTo == null || r.ValidTo >= date))
            .ToListAsync(cancellationToken);

        return ids.ToDictionary(
            id => id,
            id => (GroupCostRate?)rates
                .Where(r => r.GroupId == id)
                .OrderByDescending(r => r.ValidFrom)
                .FirstOrDefault());
    }
}
