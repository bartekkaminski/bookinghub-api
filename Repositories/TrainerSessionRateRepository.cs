using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium historii stawek godzinowych trenerów (TrainerSessionRate).
/// </summary>
public sealed class TrainerSessionRateRepository : BaseRepository<TrainerSessionRate>, ITrainerSessionRateRepository
{
    public TrainerSessionRateRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<PagedResult<TrainerSessionRate>> GetPagedAsync(TrainerSessionRateFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (filter.TrainerMemberId.HasValue)
            query = query.Where(r => r.TrainerMemberId == filter.TrainerMemberId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Currency))
            query = query.Where(r => r.Currency == filter.Currency);

        if (filter.ValidFrom.HasValue)
            query = query.Where(r => r.ValidFrom >= filter.ValidFrom.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.ValidFrom)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TrainerSessionRate>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TrainerSessionRate>> GetByTrainerAsync(Guid trainerMemberId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(r => r.TrainerMemberId == trainerMemberId)
            .OrderByDescending(r => r.ValidFrom)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<TrainerSessionRate?> GetCurrentByTrainerAsync(Guid trainerMemberId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(r => r.TrainerMemberId == trainerMemberId && r.ValidTo == null)
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<TrainerSessionRate?> GetRateOnDateAsync(Guid trainerMemberId, DateOnly date, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(r => r.TrainerMemberId == trainerMemberId
                     && r.ValidFrom <= date
                     && (r.ValidTo == null || r.ValidTo >= date))
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<Dictionary<Guid, TrainerSessionRate?>> GetRatesOnDateForTrainersAsync(IEnumerable<Guid> trainerMemberIds, DateOnly date, CancellationToken cancellationToken = default)
    {
        var ids = trainerMemberIds.ToList();
        if (ids.Count == 0) return [];

        var rates = await _dbSet
            .AsNoTracking()
            .Where(r => ids.Contains(r.TrainerMemberId)
                     && r.ValidFrom <= date
                     && (r.ValidTo == null || r.ValidTo >= date))
            .ToListAsync(cancellationToken);

        return ids.ToDictionary(
            id => id,
            id => (TrainerSessionRate?)rates
                .Where(r => r.TrainerMemberId == id)
                .OrderByDescending(r => r.ValidFrom)
                .FirstOrDefault());
    }
}
