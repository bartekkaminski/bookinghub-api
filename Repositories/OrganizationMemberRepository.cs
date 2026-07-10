using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium członkostw w organizacjach (OrganizationMember).
/// </summary>
public sealed class OrganizationMemberRepository : BaseRepository<OrganizationMember>, IOrganizationMemberRepository
{
    public OrganizationMemberRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<OrganizationMember?> GetByPersonAndOrgAsync(Guid personId, Guid organizationId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(m => m.Roles)
            .FirstOrDefaultAsync(m => m.PersonId == personId && m.OrganizationId == organizationId, cancellationToken);

    /// <inheritdoc/>
    public async Task<OrganizationMember?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(m => m.Person)
                .ThenInclude(p => p.User)
            .Include(m => m.Organization)
            .Include(m => m.Roles)
            .Include(m => m.Availability)
            .Include(m => m.GroupMemberships)
                .ThenInclude(gm => gm.Group)
            .Include(m => m.TeamMemberships)
                .ThenInclude(tm => tm.Team)
            .Include(m => m.AssignedTrainers)
                .ThenInclude(pt => pt.Trainer)
                    .ThenInclude(t => t.Person)
            .Include(m => m.AssignedTeams)
                .ThenInclude(tt => tt.Team)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<OrganizationMember?> GetWithRolesAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(m => m.Roles)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<OrganizationMember>> GetPagedAsync(OrganizationMemberFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(m => m.Person)
            .Include(m => m.Roles)
            .AsQueryable();

        if (filter.OrganizationId.HasValue)
            query = query.Where(m => m.OrganizationId == filter.OrganizationId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLowerInvariant();
            query = query.Where(m =>
                (m.DisplayName != null && m.DisplayName.ToLower().Contains(search)) ||
                (m.Person.FirstName != null && m.Person.FirstName.ToLower().Contains(search)) ||
                (m.Person.LastName  != null && m.Person.LastName.ToLower().Contains(search)));
        }

        if (filter.Role.HasValue)
            query = query.Where(m => m.Roles.Any(r => r.Role == filter.Role.Value));

        if (filter.IsActive.HasValue)
            query = query.Where(m => m.IsActive == filter.IsActive.Value);

        query = ApplySorting(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<OrganizationMember>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OrganizationMember>> GetByRoleAsync(Guid organizationId, MemberRole role, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(m => m.Person)
            .Include(m => m.Roles)
            .Where(m => m.OrganizationId == organizationId && m.Roles.Any(r => r.Role == role))
            .OrderBy(m => m.Person.LastName)
            .ThenBy(m => m.Person.FirstName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OrganizationMember>> GetTrainersAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await GetByRoleAsync(organizationId, MemberRole.Trainer, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OrganizationMember>> GetParticipantsAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await GetByRoleAsync(organizationId, MemberRole.Participant, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> IsMemberAsync(Guid personId, Guid organizationId, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(m => m.PersonId == personId && m.OrganizationId == organizationId, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> SetActiveAsync(Guid memberId, bool isActive, CancellationToken cancellationToken = default)
    {
        var member = await _dbSet.FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken);
        if (member is null) return false;

        member.IsActive = isActive;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OrganizationMember>> GetAssignedTrainersAsync(Guid participantMemberId, CancellationToken cancellationToken = default)
        => await _context.Set<ParticipantTrainer>()
            .AsNoTracking()
            .Where(pt => pt.ParticipantMemberId == participantMemberId)
            .Include(pt => pt.Trainer)
                .ThenInclude(t => t.Person)
            .Include(pt => pt.Trainer)
                .ThenInclude(t => t.Roles)
            .Select(pt => pt.Trainer)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OrganizationMember>> GetAssignedParticipantsAsync(Guid trainerMemberId, CancellationToken cancellationToken = default)
        => await _context.Set<ParticipantTrainer>()
            .AsNoTracking()
            .Where(pt => pt.TrainerMemberId == trainerMemberId)
            .Include(pt => pt.Participant)
                .ThenInclude(p => p.Person)
            .Include(pt => pt.Participant)
                .ThenInclude(p => p.Roles)
            .Select(pt => pt.Participant)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OrganizationMember>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await _context.Set<GroupMember>()
            .AsNoTracking()
            .Where(gm => gm.GroupId == groupId)
            .Include(gm => gm.OrganizationMember)
                .ThenInclude(m => m.Person)
            .Include(gm => gm.OrganizationMember)
                .ThenInclude(m => m.Roles)
            .Select(gm => gm.OrganizationMember)
            .OrderBy(m => m.Person.LastName)
            .ThenBy(m => m.Person.FirstName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OrganizationMember>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
        => await _context.Set<TeamMember>()
            .AsNoTracking()
            .Where(tm => tm.TeamId == teamId)
            .Include(tm => tm.OrganizationMember)
                .ThenInclude(m => m.Person)
            .Include(tm => tm.OrganizationMember)
                .ThenInclude(m => m.Roles)
            .Select(tm => tm.OrganizationMember)
            .OrderBy(m => m.Person.LastName)
            .ThenBy(m => m.Person.FirstName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OrganizationMember>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(m => m.Person)
            .Include(m => m.Roles)
            .Where(m => m.OrganizationId == organizationId && m.IsActive)
            .OrderBy(m => m.Person.LastName)
            .ThenBy(m => m.Person.FirstName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OrganizationMember>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(m => m.Organization)
            .Include(m => m.Roles)
            .Where(m => m.PersonId == personId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<int> CountByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(m => m.OrganizationId == organizationId, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> AnyActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(m => m.OrganizationId == organizationId && m.IsActive, cancellationToken);

    /// <inheritdoc/>
    public async Task<int> CountAdminsInOrgAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await _dbSet
            .CountAsync(m => m.OrganizationId == organizationId
                          && m.IsActive
                          && m.Roles.Any(r => r.Role == MemberRole.Admin),
                cancellationToken);

    /// <inheritdoc/>
    public async Task AddParticipantTrainerAsync(Guid participantMemberId, Guid trainerMemberId, CancellationToken cancellationToken = default)
    {
        var relation = new ParticipantTrainer
        {
            ParticipantMemberId = participantMemberId,
            TrainerMemberId     = trainerMemberId,
        };
        await _context.Set<ParticipantTrainer>().AddAsync(relation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveParticipantTrainerAsync(Guid participantMemberId, Guid trainerMemberId, CancellationToken cancellationToken = default)
    {
        var relation = await _context.Set<ParticipantTrainer>()
            .FirstOrDefaultAsync(pt => pt.ParticipantMemberId == participantMemberId
                                    && pt.TrainerMemberId == trainerMemberId, cancellationToken);
        if (relation is not null)
        {
            _context.Set<ParticipantTrainer>().Remove(relation);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ParticipantTrainerExistsAsync(Guid participantMemberId, Guid trainerMemberId, CancellationToken cancellationToken = default)
        => await _context.Set<ParticipantTrainer>()
            .AnyAsync(pt => pt.ParticipantMemberId == participantMemberId
                         && pt.TrainerMemberId == trainerMemberId, cancellationToken);

    /// <inheritdoc/>
    public async Task AddRoleDirectAsync(Guid memberId, MemberRole role, CancellationToken cancellationToken = default)
    {
        var alreadyExists = await _context.OrganizationMemberRoles
            .AnyAsync(r => r.OrganizationMemberId == memberId && r.Role == role, cancellationToken);

        if (alreadyExists) return;

        _context.OrganizationMemberRoles.Add(new OrganizationMemberRole
        {
            OrganizationMemberId = memberId,
            Role = role,
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveRoleDirectAsync(Guid memberId, MemberRole role, CancellationToken cancellationToken = default)
    {
        var existing = await _context.OrganizationMemberRoles
            .FirstOrDefaultAsync(r => r.OrganizationMemberId == memberId && r.Role == role, cancellationToken);

        if (existing is null) return;

        _context.OrganizationMemberRoles.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IQueryable<OrganizationMember> ApplySorting(IQueryable<OrganizationMember> query, OrganizationMemberFilterParams filter)
        => filter.SortBy?.ToLowerInvariant() switch
        {
            "displayname" => filter.SortDescending ? query.OrderByDescending(m => m.DisplayName ?? m.Person.LastName) : query.OrderBy(m => m.DisplayName ?? m.Person.LastName),
            "firstname"   => filter.SortDescending ? query.OrderByDescending(m => m.Person.FirstName) : query.OrderBy(m => m.Person.FirstName),
            "lastname"    => filter.SortDescending ? query.OrderByDescending(m => m.Person.LastName)  : query.OrderBy(m => m.Person.LastName),
            "priority"    => filter.SortDescending ? query.OrderByDescending(m => m.Priority)         : query.OrderBy(m => m.Priority),
            "createdat"   => filter.SortDescending ? query.OrderByDescending(m => m.CreatedAt)        : query.OrderBy(m => m.CreatedAt),
            _             => query.OrderBy(m => m.Person.LastName).ThenBy(m => m.Person.FirstName)
        };
}
