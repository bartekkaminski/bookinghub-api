using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium zajęć (Event) — konkretnych terminów w kalendarzu.
/// </summary>
public sealed class EventRepository : BaseRepository<Event>, IEventRepository
{
    public EventRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Event?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(e => e.Location)
            .Include(e => e.Group)
            .Include(e => e.Trainers)
                .ThenInclude(et => et.OrganizationMember)
                    .ThenInclude(m => m.Person)
            .Include(e => e.Enrollments)
                .ThenInclude(en => en.OrganizationMember)
                    .ThenInclude(m => m.Person)
            .Include(e => e.TeamEnrollments)
                .ThenInclude(te => te.Team)
                    .ThenInclude(t => t.Members)
                        .ThenInclude(tm => tm.OrganizationMember)
                            .ThenInclude(m => m.Person)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<Event?> GetWithTrainersAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(e => e.Trainers)
                .ThenInclude(et => et.OrganizationMember)
                    .ThenInclude(m => m.Person)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<Event?> GetWithEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(e => e.Enrollments)
                .ThenInclude(en => en.OrganizationMember)
                    .ThenInclude(m => m.Person)
            .Include(e => e.TeamEnrollments)
                .ThenInclude(te => te.Team)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<Event>> GetPagedAsync(EventFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(e => e.Location)
            .Include(e => e.Group)
            .AsQueryable();

        query = ApplyFilters(query, filter);
        query = ApplySorting(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Event>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Event>> GetCalendarAsync(Guid organizationId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(e => e.Group)
            .Include(e => e.Location)
            .Include(e => e.Trainers)
                .ThenInclude(et => et.OrganizationMember)
                    .ThenInclude(m => m.Person)
            .Where(e => e.OrganizationId == organizationId && e.StartTime >= from && e.StartTime <= to)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Event>> GetCalendarForMemberAsync(Guid organizationMemberId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        // Pobieramy zajęcia gdzie uczestnik jest zapisany indywidualnie lub przez zespół
        var individualEventIds = await _context.Set<EventEnrollment>()
            .AsNoTracking()
            .Where(en => en.OrganizationMemberId == organizationMemberId
                      && en.Status != EventEnrollmentStatus.Cancelled)
            .Select(en => en.EventId)
            .ToListAsync(cancellationToken);

        var teamIds = await _context.Set<TeamMember>()
            .AsNoTracking()
            .Where(tm => tm.OrganizationMemberId == organizationMemberId)
            .Select(tm => tm.TeamId)
            .ToListAsync(cancellationToken);

        var teamEventIds = await _context.Set<EventTeamEnrollment>()
            .AsNoTracking()
            .Where(te => teamIds.Contains(te.TeamId)
                      && te.Status != EventEnrollmentStatus.Cancelled)
            .Select(te => te.EventId)
            .ToListAsync(cancellationToken);

        var allEventIds = individualEventIds.Union(teamEventIds).Distinct().ToList();

        return await _dbSet
            .AsNoTracking()
            .Include(e => e.Group)
            .Include(e => e.Location)
            .Include(e => e.Trainers)
                .ThenInclude(et => et.OrganizationMember)
                    .ThenInclude(m => m.Person)
            .Where(e => allEventIds.Contains(e.Id) && e.StartTime >= from && e.StartTime <= to)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Event>> GetBySeriesGroupAsync(Guid seriesGroupId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(e => e.Location)
            .Include(e => e.Group)
            .Where(e => e.SeriesGroupId == seriesGroupId)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Event>> GetUpcomingAsync(Guid organizationId, int count = 10, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .AsNoTracking()
            .Include(e => e.Group)
            .Include(e => e.Location)
            .Where(e => e.OrganizationId == organizationId
                     && e.StartTime >= now
                     && e.Status == EventStatus.Scheduled)
            .OrderBy(e => e.StartTime)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> SetStatusAsync(Guid eventId, EventStatus status, CancellationToken cancellationToken = default)
    {
        var ev = await _dbSet.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (ev is null) return false;

        ev.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Event>> GetByTrainerAsync(Guid trainerMemberId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(e => e.Group)
            .Include(e => e.Location)
            .Where(e => e.Trainers.Any(et => et.OrganizationMemberId == trainerMemberId)
                     && e.StartTime >= from
                     && e.StartTime <= to)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> IsTrainerAssignedAsync(Guid eventId, Guid trainerMemberId, CancellationToken cancellationToken = default)
        => await _context.Set<EventTrainer>()
            .AnyAsync(et => et.EventId == eventId && et.OrganizationMemberId == trainerMemberId, cancellationToken);

    /// <inheritdoc/>
    public async Task AddTrainerAsync(Guid eventId, Guid trainerMemberId, CancellationToken cancellationToken = default)
    {
        var et = new EventTrainer { EventId = eventId, OrganizationMemberId = trainerMemberId };
        await _context.Set<EventTrainer>().AddAsync(et, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveTrainerAsync(Guid eventId, Guid trainerMemberId, CancellationToken cancellationToken = default)
    {
        var et = await _context.Set<EventTrainer>()
            .FirstOrDefaultAsync(x => x.EventId == eventId && x.OrganizationMemberId == trainerMemberId, cancellationToken);
        if (et is not null)
        {
            _context.Set<EventTrainer>().Remove(et);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<int> CountByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(e => e.GroupId == groupId, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Event>> GetByLocationAndRangeAsync(
        Guid locationId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(e => e.Group)
            .Include(e => e.Enrollments)
            .Include(e => e.TeamEnrollments)
                .ThenInclude(te => te.Team)
                    .ThenInclude(t => t.Members)
            .Where(e => e.LocationId == locationId
                     && e.StartTime < to
                     && e.EndTime > from)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IQueryable<Event> ApplyFilters(IQueryable<Event> query, EventFilterParams filter)
    {
        if (filter.OrganizationId.HasValue)
            query = query.Where(e => e.OrganizationId == filter.OrganizationId.Value);

        if (filter.SeriesGroupId.HasValue)
            query = query.Where(e => e.SeriesGroupId == filter.SeriesGroupId.Value);

        if (filter.GroupId.HasValue)
            query = query.Where(e => e.GroupId == filter.GroupId.Value);

        if (filter.LocationId.HasValue)
            query = query.Where(e => e.LocationId == filter.LocationId.Value);

        if (filter.EventType.HasValue)
            query = query.Where(e => e.EventType == filter.EventType.Value);

        if (filter.Status.HasValue)
            query = query.Where(e => e.Status == filter.Status.Value);

        if (filter.StartFrom.HasValue)
            query = query.Where(e => e.StartTime >= filter.StartFrom.Value);

        if (filter.StartTo.HasValue)
            query = query.Where(e => e.StartTime <= filter.StartTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLowerInvariant();
            query = query.Where(e => e.Title.ToLower().Contains(search));
        }

        return query;
    }

    private static IQueryable<Event> ApplySorting(IQueryable<Event> query, EventFilterParams filter)
        => filter.SortBy?.ToLowerInvariant() switch
        {
            "title"     => filter.SortDescending ? query.OrderByDescending(e => e.Title)     : query.OrderBy(e => e.Title),
            "starttime" => filter.SortDescending ? query.OrderByDescending(e => e.StartTime) : query.OrderBy(e => e.StartTime),
            "endtime"   => filter.SortDescending ? query.OrderByDescending(e => e.EndTime)   : query.OrderBy(e => e.EndTime),
            "status"    => filter.SortDescending ? query.OrderByDescending(e => e.Status)    : query.OrderBy(e => e.Status),
            "createdat" => filter.SortDescending ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt),
            _           => query.OrderBy(e => e.StartTime)
        };
}
