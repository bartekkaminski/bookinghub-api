using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium grup zajęciowych (Group).
/// </summary>
public sealed class GroupRepository : BaseRepository<Group>, IGroupRepository
{
    public GroupRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Group?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(g => g.Members)
                .ThenInclude(gm => gm.OrganizationMember)
                    .ThenInclude(m => m.Person)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<Group?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(g => g.Members)
                .ThenInclude(gm => gm.OrganizationMember)
                    .ThenInclude(m => m.Person)
            .Include(g => g.Teams)
                .ThenInclude(tg => tg.Team)
                    .ThenInclude(t => t.Members)
            .Include(g => g.CostRates)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<Group>> GetPagedAsync(GroupFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (filter.OrganizationId.HasValue)
            query = query.Where(g => g.OrganizationId == filter.OrganizationId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLowerInvariant();
            query = query.Where(g => g.Name.ToLower().Contains(search));
        }

        if (filter.IsActive.HasValue)
            query = query.Where(g => g.IsActive == filter.IsActive.Value);

        query = ApplySorting(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(g => g.Members)
            .Include(g => g.Teams)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Group>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Group>> GetByOrganizationAsync(Guid organizationId, bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(g => g.OrganizationId == organizationId);
        if (onlyActive) query = query.Where(g => g.IsActive);
        return await query.OrderBy(g => g.Name).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Group>> GetByMemberAsync(Guid organizationMemberId, CancellationToken cancellationToken = default)
        => await _context.Set<GroupMember>()
            .AsNoTracking()
            .Where(gm => gm.OrganizationMemberId == organizationMemberId)
            .Include(gm => gm.Group)
            .Select(gm => gm.Group)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Group>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
        => await _context.Set<TeamGroup>()
            .AsNoTracking()
            .Where(tg => tg.TeamId == teamId)
            .Include(tg => tg.Group)
            .Select(tg => tg.Group)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> IsNameTakenInOrgAsync(Guid organizationId, string name, Guid? excludeGroupId = null, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(
            g => g.OrganizationId == organizationId
              && g.Name == name
              && (excludeGroupId == null || g.Id != excludeGroupId),
            cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> IsMemberInGroupAsync(Guid groupId, Guid organizationMemberId, CancellationToken cancellationToken = default)
        => await _context.Set<GroupMember>()
            .AnyAsync(gm => gm.GroupId == groupId && gm.OrganizationMemberId == organizationMemberId, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> IsTeamInGroupAsync(Guid groupId, Guid teamId, CancellationToken cancellationToken = default)
        => await _context.Set<TeamGroup>()
            .AnyAsync(tg => tg.GroupId == groupId && tg.TeamId == teamId, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> IsNameTakenAsync(Guid organizationId, string name, Guid? excludeGroupId = null, CancellationToken cancellationToken = default)
        => IsNameTakenInOrgAsync(organizationId, name, excludeGroupId, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> HasUpcomingEventsAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await _context.Set<Event>()
            .AnyAsync(e => e.GroupId == groupId && e.StartTime > DateTime.UtcNow && e.Status == EventStatus.Scheduled,
                cancellationToken);

    /// <inheritdoc/>
    public async Task AddMemberAsync(Guid groupId, Guid organizationMemberId, CancellationToken cancellationToken = default)
    {
        var gm = new GroupMember { GroupId = groupId, OrganizationMemberId = organizationMemberId, JoinedAt = DateTime.UtcNow };
        await _context.Set<GroupMember>().AddAsync(gm, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveMemberAsync(Guid groupId, Guid organizationMemberId, CancellationToken cancellationToken = default)
    {
        var gm = await _context.Set<GroupMember>()
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.OrganizationMemberId == organizationMemberId, cancellationToken);
        if (gm is not null)
        {
            _context.Set<GroupMember>().Remove(gm);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task AddTeamAsync(Guid groupId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var tg = new TeamGroup { GroupId = groupId, TeamId = teamId };
        await _context.Set<TeamGroup>().AddAsync(tg, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveTeamAsync(Guid groupId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var tg = await _context.Set<TeamGroup>()
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.TeamId == teamId, cancellationToken);
        if (tg is not null)
        {
            _context.Set<TeamGroup>().Remove(tg);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<int> CountByOrganizationAsync(Guid organizationId, bool activeOnly = false, CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(
            g => g.OrganizationId == organizationId && (!activeOnly || g.IsActive),
            cancellationToken);

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IQueryable<Group> ApplySorting(IQueryable<Group> query, GroupFilterParams filter)
        => filter.SortBy?.ToLowerInvariant() switch
        {
            "name"      => filter.SortDescending ? query.OrderByDescending(g => g.Name)      : query.OrderBy(g => g.Name),
            "createdat" => filter.SortDescending ? query.OrderByDescending(g => g.CreatedAt) : query.OrderBy(g => g.CreatedAt),
            _           => query.OrderBy(g => g.Name)
        };
}
