using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

public sealed class UserDeviceTokenRepository : IUserDeviceTokenRepository
{
    private readonly AppDbContext _db;

    public UserDeviceTokenRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserDeviceToken>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.UserDeviceTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<UserDeviceToken> AddAsync(UserDeviceToken token, CancellationToken ct = default)
    {
        _db.UserDeviceTokens.Add(token);
        await _db.SaveChangesAsync(ct);
        return token;
    }

    public async Task<bool> DeleteAsync(Guid userId, string token, CancellationToken ct = default)
    {
        var existing = await _db.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token, ct);

        if (existing is null) return false;

        _db.UserDeviceTokens.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid userId, string token, CancellationToken ct = default)
        => await _db.UserDeviceTokens
            .AnyAsync(t => t.UserId == userId && t.Token == token, ct);

    public async Task UpdateLastSeenAsync(Guid userId, DateTime seenAt, CancellationToken ct = default)
        => await _db.UserDeviceTokens
            .Where(t => t.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.LastSeenAt, seenAt), ct);
}
