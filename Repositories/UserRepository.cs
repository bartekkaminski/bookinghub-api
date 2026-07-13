using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium tożsamości logowania (User).
/// </summary>
public sealed class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<User?> GetByExternalIdAsync(string externalId, string authProvider, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId && u.AuthProvider == authProvider, cancellationToken);

    /// <inheritdoc/>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    /// <inheritdoc/>
    public async Task<User?> GetWithPersonAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(u => u.Person)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<User>> GetPagedAsync(UserFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLowerInvariant();
            query = query.Where(u => u.Email.ToLower().Contains(search));
        }

        if (filter.IsActive.HasValue)
            query = query.Where(u => u.IsActive == filter.IsActive.Value);

        query = ApplySorting(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<User>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<bool> IsEmailTakenAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var normalised = email.ToLowerInvariant();
        return await _dbSet.AnyAsync(
            u => u.Email == normalised && (excludeUserId == null || u.Id != excludeUserId),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsExternalIdTakenAsync(string externalId, string authProvider, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(
            u => u.ExternalId == externalId
              && u.AuthProvider == authProvider
              && (excludeUserId == null || u.Id != excludeUserId),
            cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _dbSet.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return false;

        user.IsActive = isActive;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<User?> GetByExternalIdIgnoreFiltersAsync(string externalId, string authProvider, CancellationToken cancellationToken = default)
        => await _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(u => u.Person)
            .FirstOrDefaultAsync(u => u.ExternalId == externalId && u.AuthProvider == authProvider, cancellationToken);

    /// <inheritdoc/>
    public async Task<User?> GetByProfileCodeAsync(string profileCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileCode)) return null;
        var normalised = profileCode.Trim().ToUpperInvariant();
        return await _dbSet
            .AsNoTracking()
            .Include(u => u.Person)
            .FirstOrDefaultAsync(u => u.ProfileCode == normalised, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ProfileCodeExistsAsync(string profileCode, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(u => u.ProfileCode == profileCode, cancellationToken);

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IQueryable<User> ApplySorting(IQueryable<User> query, UserFilterParams filter)
        => filter.SortBy?.ToLowerInvariant() switch
        {
            "email"     => filter.SortDescending ? query.OrderByDescending(u => u.Email)     : query.OrderBy(u => u.Email),
            "createdat" => filter.SortDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            "isactive"  => filter.SortDescending ? query.OrderByDescending(u => u.IsActive)  : query.OrderBy(u => u.IsActive),
            _           => query.OrderBy(u => u.Email)
        };
}
