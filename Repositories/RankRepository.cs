using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium rang organizacyjnych.
/// </summary>
public sealed class RankRepository : BaseRepository<OrganizationRank>, IRankRepository
{
    public RankRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OrganizationRank>> GetByOrganizationAsync(
        Guid organizationId, CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<PagedResult<OrganizationMember>> GetPagedMembersAsync(
        Guid rankId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Set<OrganizationMember>()
            .AsNoTracking()
            .Where(m => m.RankId == rankId)
            .Include(m => m.Person)
            .Include(m => m.Roles)
            .OrderBy(m => m.Person.LastName)
            .ThenBy(m => m.Person.FirstName);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<OrganizationMember>(items, page, pageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<int> CountMembersAsync(Guid rankId, CancellationToken ct = default)
        => await _context.Set<OrganizationMember>()
            .CountAsync(m => m.RankId == rankId, ct);

    /// <inheritdoc/>
    public async Task<bool> IsNameTakenAsync(
        Guid organizationId, string name, Guid? excludeId = null, CancellationToken ct = default)
        => await _dbSet.AnyAsync(
            r => r.OrganizationId == organizationId
              && r.Name == name
              && (excludeId == null || r.Id != excludeId),
            ct);
}
