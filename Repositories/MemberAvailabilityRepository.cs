using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium slotów dostępności trenerów i uczestników (MemberAvailability).
/// </summary>
public sealed class MemberAvailabilityRepository : BaseRepository<MemberAvailability>, IMemberAvailabilityRepository
{
    public MemberAvailabilityRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<PagedResult<MemberAvailability>> GetPagedAsync(MemberAvailabilityFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (filter.OrganizationMemberId.HasValue)
            query = query.Where(a => a.OrganizationMemberId == filter.OrganizationMemberId.Value);

        if (filter.DayOfWeek.HasValue)
            query = query.Where(a => a.DayOfWeek == filter.DayOfWeek.Value);

        if (filter.ActiveOnDate.HasValue)
        {
            var date = filter.ActiveOnDate.Value;
            query = query.Where(a =>
                (a.ValidFrom == null || a.ValidFrom <= date) &&
                (a.ValidTo   == null || a.ValidTo   >= date));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.TimeFrom)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MemberAvailability>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MemberAvailability>> GetByMemberAsync(Guid organizationMemberId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(a => a.OrganizationMemberId == organizationMemberId)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.TimeFrom)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MemberAvailability>> GetByMemberOnDateAsync(Guid organizationMemberId, DateOnly date, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(a => a.OrganizationMemberId == organizationMemberId
                     && (a.ValidFrom == null || a.ValidFrom <= date)
                     && (a.ValidTo   == null || a.ValidTo   >= date))
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.TimeFrom)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MemberAvailability>> GetByMemberAndDayAsync(Guid organizationMemberId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(a => a.OrganizationMemberId == organizationMemberId && a.DayOfWeek == dayOfWeek)
            .OrderBy(a => a.TimeFrom)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MemberAvailability>> GetByMembersOnDayAsync(
        IEnumerable<Guid> organizationMemberIds,
        DayOfWeek dayOfWeek,
        DateOnly? onDate = null,
        CancellationToken cancellationToken = default)
    {
        var ids = organizationMemberIds.ToList();
        if (ids.Count == 0) return [];

        var query = _dbSet
            .AsNoTracking()
            .Where(a => ids.Contains(a.OrganizationMemberId) && a.DayOfWeek == dayOfWeek);

        if (onDate.HasValue)
        {
            var date = onDate.Value;
            query = query.Where(a =>
                (a.ValidFrom == null || a.ValidFrom <= date) &&
                (a.ValidTo   == null || a.ValidTo   >= date));
        }

        return await query
            .OrderBy(a => a.OrganizationMemberId)
            .ThenBy(a => a.TimeFrom)
            .ToListAsync(cancellationToken);
    }
}
