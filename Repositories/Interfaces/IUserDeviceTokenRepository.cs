using BookingHub.Api.Models;

namespace BookingHub.Api.Repositories.Interfaces;


public interface IUserDeviceTokenRepository
{
    Task<IReadOnlyList<UserDeviceToken>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserDeviceToken> AddAsync(UserDeviceToken token, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, string token, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, string token, CancellationToken ct = default);

    /// <summary>
    /// Usuwa wszystkie tokeny danego użytkownika dla wskazanej platformy z wyjątkiem podanego tokenu.
    /// Wywoływane przed rejestracją nowego tokenu — zapobiega duplikatom i podwójnym powiadomieniom.
    /// </summary>
    Task DeleteOtherPlatformTokensAsync(Guid userId, DevicePlatform platform, string keepToken, CancellationToken ct = default);

    /// <summary>
    /// Aktualizuje LastSeenAt wyłącznie dla konkretnego tokenu FCM (identyfikowanego przez token string).
    /// Wywoływane przez AppHub.Heartbeat() co 60 sekund — aktualizuje tylko urządzenie, które
    /// wysłało heartbeat, a nie wszystkie tokeny użytkownika (desktop nie "ożywia" telefonu).
    /// </summary>
    Task UpdateLastSeenByTokenAsync(Guid userId, string fcmToken, DateTime seenAt, CancellationToken ct = default);
}
