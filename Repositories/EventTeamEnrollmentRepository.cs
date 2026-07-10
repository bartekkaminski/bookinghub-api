using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium zapisów całych zespołów na zajęcia (EventTeamEnrollment).
/// </summary>
public sealed class EventTeamEnrollmentRepository : BaseRepository<EventTeamEnrollment>, IEventTeamEnrollmentRepository
{
    public EventTeamEnrollmentRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<EventTeamEnrollment?> GetByEventAndTeamAsync(Guid eventId, Guid teamId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(te => te.EventId == eventId && te.TeamId == teamId, cancellationToken);

    /// <inheritdoc/>
    public async Task<EventTeamEnrollment?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(te => te.Event)
                .ThenInclude(e => e.Location)
            .Include(te => te.Team)
                .ThenInclude(t => t.Members)
                    .ThenInclude(tm => tm.OrganizationMember)
                        .ThenInclude(m => m.Person)
            .FirstOrDefaultAsync(te => te.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<EventTeamEnrollment>> GetPagedAsync(EventTeamEnrollmentFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Include(te => te.Team)
            .AsQueryable();

        if (filter.EventId.HasValue)
            query = query.Where(te => te.EventId == filter.EventId.Value);

        if (filter.TeamId.HasValue)
            query = query.Where(te => te.TeamId == filter.TeamId.Value);

        if (filter.Status.HasValue)
            query = query.Where(te => te.Status == filter.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(te => te.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EventTeamEnrollment>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EventTeamEnrollment>> GetByEventAsync(Guid eventId, EventEnrollmentStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Include(te => te.Team)
                .ThenInclude(t => t.Members)
                    .ThenInclude(tm => tm.OrganizationMember)
                        .ThenInclude(m => m.Person)
            .Where(te => te.EventId == eventId);

        if (status.HasValue)
            query = query.Where(te => te.Status == status.Value);

        return await query.OrderBy(te => te.Team.Priority).ThenBy(te => te.Team.Name).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EventTeamEnrollment>> GetByTeamAsync(Guid teamId, EventEnrollmentStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Include(te => te.Event)
                .ThenInclude(e => e.Location)
            .Where(te => te.TeamId == teamId);

        if (status.HasValue)
            query = query.Where(te => te.Status == status.Value);

        return await query.OrderByDescending(te => te.Event.StartTime).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsEnrolledAsync(Guid eventId, Guid teamId, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(
            te => te.EventId == eventId
               && te.TeamId == teamId
               && te.Status != EventEnrollmentStatus.Cancelled,
            cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> SetStatusAsync(Guid enrollmentId, EventEnrollmentStatus status, CancellationToken cancellationToken = default)
    {
        var enrollment = await _dbSet.FirstOrDefaultAsync(te => te.Id == enrollmentId, cancellationToken);
        if (enrollment is null) return false;

        enrollment.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
