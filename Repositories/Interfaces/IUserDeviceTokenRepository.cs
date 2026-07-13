using BookingHub.Api.Models;

namespace BookingHub.Api.Repositories.Interfaces;

public interface IUserDeviceTokenRepository
{
    Task<IReadOnlyList<UserDeviceToken>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserDeviceToken> AddAsync(UserDeviceToken token, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, string token, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, string token, CancellationToken ct = default);

    /// <summary>
    /// Aktualizuje LastSeenAt dla wszystkich tokenów FCM danego użytkownika.
    /// Wywoływane przez AppHub.Heartbeat() co 60 sekund.
    /// Używane przez FcmService do detekcji online/offline.
    /// </summary>
    Task UpdateLastSeenAsync(Guid userId, DateTime seenAt, CancellationToken ct = default);
}
