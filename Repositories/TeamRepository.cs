using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium zespołów (par, formacji, drużyn).
/// </summary>
public sealed class TeamRepository : BaseRepository<Team>, ITeamRepository
{
    public TeamRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Team?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(t => t.Members)
                .ThenInclude(tm => tm.OrganizationMember)
                    .ThenInclude(m => m.Person)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<Team?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(t => t.Members)
                .ThenInclude(tm => tm.OrganizationMember)
                    .ThenInclude(m => m.Person)
            .Include(t => t.Groups)
                .ThenInclude(tg => tg.Group)
            .Include(t => t.Trainers)
                .ThenInclude(tt => tt.Trainer)
                    .ThenInclude(tr => tr.Person)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<Team>> GetPagedAsync(TeamFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (filter.OrganizationId.HasValue)
            query = query.Where(t => t.OrganizationId == filter.OrganizationId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLowerInvariant();
            query = query.Where(t => t.Name != null && t.Name.ToLower().Contains(search));
        }

        if (filter.IsActive.HasValue)
            query = query.Where(t => t.IsActive == filter.IsActive.Value);

        if (filter.GroupId.HasValue)
            query = query.Where(t => t.Groups.Any(tg => tg.GroupId == filter.GroupId.Value));

        query = ApplySorting(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Team>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Team>> GetByOrganizationAsync(Guid organizationId, bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(t => t.OrganizationId == organizationId);
        if (onlyActive) query = query.Where(t => t.IsActive);
        return await query
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Team>> GetByMemberAsync(Guid organizationMemberId, CancellationToken cancellationToken = default)
        => await _context.Set<TeamMember>()
            .AsNoTracking()
            .Where(tm => tm.OrganizationMemberId == organizationMemberId)
            .Include(tm => tm.Team)
            .Select(tm => tm.Team)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Team>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await _context.Set<TeamGroup>()
            .AsNoTracking()
            .Where(tg => tg.GroupId == groupId)
            .Include(tg => tg.Team)
                .ThenInclude(t => t.Members)
                    .ThenInclude(tm => tm.OrganizationMember)
                        .ThenInclude(m => m.Person)
            .Select(tg => tg.Team)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Team>> GetByTrainerAsync(Guid trainerMemberId, CancellationToken cancellationToken = default)
        => await _context.Set<TeamTrainer>()
            .AsNoTracking()
            .Where(tt => tt.TrainerMemberId == trainerMemberId)
            .Include(tt => tt.Team)
            .Select(tt => tt.Team)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> IsMemberInTeamAsync(Guid teamId, Guid organizationMemberId, CancellationToken cancellationToken = default)
        => await _context.Set<TeamMember>()
            .AnyAsync(tm => tm.TeamId == teamId && tm.OrganizationMemberId == organizationMemberId, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> IsTrainerAssignedAsync(Guid teamId, Guid trainerMemberId, CancellationToken cancellationToken = default)
        => await _context.Set<TeamTrainer>()
            .AnyAsync(tt => tt.TeamId == teamId && tt.TrainerMemberId == trainerMemberId, cancellationToken);

    /// <inheritdoc/>
    public async Task<int> CountByOrganizationAsync(Guid organizationId, bool activeOnly = false, CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(
            t => t.OrganizationId == organizationId && (!activeOnly || t.IsActive),
            cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> HasActiveEnrollmentsAsync(Guid teamId, CancellationToken cancellationToken = default)
        => await _context.Set<EventTeamEnrollment>()
            .AnyAsync(te => te.TeamId == teamId && te.Status == EventEnrollmentStatus.Enrolled, cancellationToken);

    /// <inheritdoc/>
    public async Task AddMemberAsync(Guid teamId, Guid organizationMemberId, CancellationToken cancellationToken = default)
    {
        var tm = new TeamMember { TeamId = teamId, OrganizationMemberId = organizationMemberId };
        await _context.Set<TeamMember>().AddAsync(tm, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveMemberAsync(Guid teamId, Guid organizationMemberId, CancellationToken cancellationToken = default)
    {
        var tm = await _context.Set<TeamMember>()
            .FirstOrDefaultAsync(x => x.TeamId == teamId && x.OrganizationMemberId == organizationMemberId, cancellationToken);
        if (tm is not null)
        {
            _context.Set<TeamMember>().Remove(tm);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task AddTrainerAsync(Guid teamId, Guid trainerMemberId, CancellationToken cancellationToken = default)
    {
        var tt = new TeamTrainer { TeamId = teamId, TrainerMemberId = trainerMemberId };
        await _context.Set<TeamTrainer>().AddAsync(tt, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveTrainerAsync(Guid teamId, Guid trainerMemberId, CancellationToken cancellationToken = default)
    {
        var tt = await _context.Set<TeamTrainer>()
            .FirstOrDefaultAsync(x => x.TeamId == teamId && x.TrainerMemberId == trainerMemberId, cancellationToken);
        if (tt is not null)
        {
            _context.Set<TeamTrainer>().Remove(tt);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IQueryable<Team> ApplySorting(IQueryable<Team> query, TeamFilterParams filter)
        => filter.SortBy?.ToLowerInvariant() switch
        {
            "name"      => filter.SortDescending ? query.OrderByDescending(t => t.Name)      : query.OrderBy(t => t.Name),
            "priority"  => filter.SortDescending ? query.OrderByDescending(t => t.Priority)  : query.OrderBy(t => t.Priority),
            "createdat" => filter.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            _           => query.OrderBy(t => t.Priority).ThenBy(t => t.Name)
        };
}
