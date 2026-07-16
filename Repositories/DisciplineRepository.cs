using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium dyscyplin organizacyjnych.
/// </summary>
public sealed class DisciplineRepository : BaseRepository<Discipline>, IDisciplineRepository
{
    public DisciplineRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Discipline>> GetByOrganizationAsync(
        Guid organizationId, CancellationToken ct = default)
        => await _dbSet
            .AsNoTracking()
            .Where(d => d.OrganizationId == organizationId)
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<int> CountRanksAsync(Guid disciplineId, CancellationToken ct = default)
        => await _context.Set<OrganizationRank>()
            .CountAsync(r => r.DisciplineId == disciplineId, ct);

    /// <inheritdoc/>
    public async Task<bool> IsNameTakenAsync(
        Guid organizationId, string name, Guid? excludeId = null, CancellationToken ct = default)
        => await _dbSet.AnyAsync(
            d => d.OrganizationId == organizationId
              && d.Name == name
              && (excludeId == null || d.Id != excludeId),
            ct);
}
