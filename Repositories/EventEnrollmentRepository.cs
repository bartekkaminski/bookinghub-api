using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium indywidualnych zapisów uczestników na zajęcia (EventEnrollment).
/// </summary>
public sealed class EventEnrollmentRepository : BaseRepository<EventEnrollment>, IEventEnrollmentRepository
{
    public EventEnrollmentRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<EventEnrollment?> GetByEventAndMemberAsync(Guid eventId, Guid organizationMemberId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(en => en.EventId == eventId && en.OrganizationMemberId == organizationMemberId, cancellationToken);

    /// <inheritdoc/>
    public async Task<EventEnrollment?> GetWithCancellationRequestsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(en => en.CancellationRequests.OrderByDescending(cr => cr.RequestedAt))
            .FirstOrDefaultAsync(en => en.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<EventEnrollment?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(en => en.Event)
                .ThenInclude(e => e.Location)
            .Include(en => en.OrganizationMember)
                .ThenInclude(m => m.Person)
            .Include(en => en.CancellationRequests.OrderByDescending(cr => cr.RequestedAt))
            .FirstOrDefaultAsync(en => en.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<EventEnrollment>> GetPagedAsync(EventEnrollmentFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Include(en => en.OrganizationMember)
                .ThenInclude(m => m.Person)
            .AsQueryable();

        if (filter.EventId.HasValue)
            query = query.Where(en => en.EventId == filter.EventId.Value);

        if (filter.OrganizationMemberId.HasValue)
            query = query.Where(en => en.OrganizationMemberId == filter.OrganizationMemberId.Value);

        if (filter.Status.HasValue)
            query = query.Where(en => en.Status == filter.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(en => en.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EventEnrollment>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EventEnrollment>> GetByEventAsync(Guid eventId, EventEnrollmentStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Include(en => en.OrganizationMember)
                .ThenInclude(m => m.Person)
            .Where(en => en.EventId == eventId);

        if (status.HasValue)
            query = query.Where(en => en.Status == status.Value);

        return await query
            .OrderBy(en => en.OrganizationMember.Person.LastName)
            .ThenBy(en => en.OrganizationMember.Person.FirstName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EventEnrollment>> GetByMemberAsync(Guid organizationMemberId, EventEnrollmentStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Include(en => en.Event)
                .ThenInclude(e => e.Location)
            .Where(en => en.OrganizationMemberId == organizationMemberId);

        if (status.HasValue)
            query = query.Where(en => en.Status == status.Value);

        return await query
            .OrderByDescending(en => en.Event.StartTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsEnrolledAsync(Guid eventId, Guid organizationMemberId, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(
            en => en.EventId == eventId
               && en.OrganizationMemberId == organizationMemberId
               && en.Status != EventEnrollmentStatus.Cancelled,
            cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> SetStatusAsync(Guid enrollmentId, EventEnrollmentStatus status, CancellationToken cancellationToken = default)
    {
        var enrollment = await _dbSet.FirstOrDefaultAsync(en => en.Id == enrollmentId, cancellationToken);
        if (enrollment is null) return false;

        enrollment.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<int> GetEnrolledCountAsync(Guid eventId, CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(
            en => en.EventId == eventId && en.Status == EventEnrollmentStatus.Enrolled,
            cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<EventEnrollment>> GetByMemberPagedAsync(Guid organizationMemberId, EventEnrollmentFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Include(en => en.Event)
                .ThenInclude(e => e.Location)
            .Where(en => en.OrganizationMemberId == organizationMemberId);

        if (filter.Status.HasValue)
            query = query.Where(en => en.Status == filter.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(en => en.Event.StartTime)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EventEnrollment>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<EventEnrollment?> GetActiveByEventAndMemberAsync(Guid eventId, Guid organizationMemberId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                en => en.EventId == eventId
                   && en.OrganizationMemberId == organizationMemberId
                   && en.Status == EventEnrollmentStatus.Enrolled,
                cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EventEnrollment>> GetAttendedByMemberInPeriodAsync(Guid organizationMemberId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(en => en.Event)
            .Where(en => en.OrganizationMemberId == organizationMemberId
                      && en.Status == EventEnrollmentStatus.Attended
                      && en.Event.StartTime >= from
                      && en.Event.StartTime <= to)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task BulkSetAttendanceAsync(Guid eventId, IEnumerable<Guid> presentMemberIds, CancellationToken cancellationToken = default)
    {
        var enrollments = await _dbSet
            .Where(en => en.EventId == eventId && en.Status == EventEnrollmentStatus.Enrolled)
            .ToListAsync(cancellationToken);

        var presentSet = new HashSet<Guid>(presentMemberIds);

        foreach (var enrollment in enrollments)
        {
            enrollment.Status = presentSet.Contains(enrollment.OrganizationMemberId)
                ? EventEnrollmentStatus.Attended
                : EventEnrollmentStatus.Absent;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EventEnrollment>> GetByMemberInRangeAsync(Guid organizationMemberId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(en => en.Event)
            .Where(en => en.OrganizationMemberId == organizationMemberId
                      && en.Event.StartTime >= from
                      && en.Event.StartTime <= to)
            .ToListAsync(cancellationToken);
}
