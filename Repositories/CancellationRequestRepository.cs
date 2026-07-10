using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium wniosków o odwołanie zapisu na zajęcia (CancellationRequest).
/// </summary>
public sealed class CancellationRequestRepository : BaseRepository<CancellationRequest>, ICancellationRequestRepository
{
    public CancellationRequestRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<CancellationRequest?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(cr => cr.EventEnrollment)
                .ThenInclude(en => en.Event)
                    .ThenInclude(e => e.Location)
            .Include(cr => cr.EventEnrollment)
                .ThenInclude(en => en.OrganizationMember)
                    .ThenInclude(m => m.Person)
            .Include(cr => cr.RequestedBy)
                .ThenInclude(m => m.Person)
            .Include(cr => cr.ReviewedBy)
            .FirstOrDefaultAsync(cr => cr.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<CancellationRequest>> GetPagedAsync(CancellationRequestFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Include(cr => cr.EventEnrollment)
                .ThenInclude(en => en.Event)
            .Include(cr => cr.RequestedBy)
                .ThenInclude(m => m.Person)
            .AsQueryable();

        // Wymuszenie filtrowania po org — serwis ustawia to server-side.
        if (filter.OrganizationId.HasValue)
            query = query.Where(cr => cr.EventEnrollment.Event.OrganizationId == filter.OrganizationId.Value);

        if (filter.EventEnrollmentId.HasValue)
            query = query.Where(cr => cr.EventEnrollmentId == filter.EventEnrollmentId.Value);

        if (filter.RequestedByMemberId.HasValue)
            query = query.Where(cr => cr.RequestedByMemberId == filter.RequestedByMemberId.Value);

        if (filter.Status.HasValue)
            query = query.Where(cr => cr.Status == filter.Status.Value);

        if (filter.RequestedFrom.HasValue)
            query = query.Where(cr => cr.RequestedAt >= filter.RequestedFrom.Value);

        if (filter.RequestedTo.HasValue)
            query = query.Where(cr => cr.RequestedAt <= filter.RequestedTo.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(cr => cr.RequestedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CancellationRequest>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CancellationRequest>> GetByEnrollmentAsync(Guid eventEnrollmentId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Where(cr => cr.EventEnrollmentId == eventEnrollmentId)
            .OrderByDescending(cr => cr.RequestedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CancellationRequest>> GetPendingByMemberAsync(Guid organizationMemberId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(cr => cr.EventEnrollment)
                .ThenInclude(en => en.Event)
            .Where(cr => cr.RequestedByMemberId == organizationMemberId && cr.Status == CancellationStatus.Pending)
            .OrderByDescending(cr => cr.RequestedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CancellationRequest>> GetPendingByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(cr => cr.EventEnrollment)
                .ThenInclude(en => en.Event)
            .Include(cr => cr.RequestedBy)
                .ThenInclude(m => m.Person)
            .Where(cr => cr.Status == CancellationStatus.Pending
                      && cr.EventEnrollment.Event.OrganizationId == organizationId)
            .OrderBy(cr => cr.EventEnrollment.Event.StartTime)
            .ThenBy(cr => cr.RequestedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> HasPendingRequestAsync(Guid eventEnrollmentId, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(
            cr => cr.EventEnrollmentId == eventEnrollmentId && cr.Status == CancellationStatus.Pending,
            cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> ReviewAsync(Guid requestId, CancellationStatus decision, Guid reviewedByPersonId, string? reviewNote, CancellationToken cancellationToken = default)
    {
        var request = await _dbSet.FirstOrDefaultAsync(cr => cr.Id == requestId, cancellationToken);
        if (request is null) return false;

        request.Status            = decision;
        request.ReviewedByPersonId = reviewedByPersonId;
        request.ReviewedAt        = DateTime.UtcNow;
        request.ReviewNote        = reviewNote;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
