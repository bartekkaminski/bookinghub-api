namespace BookingHub.Api.Models;

/// <summary>Token FCM urządzenia użytkownika do wysyłania push notifications.</summary>
public class UserDeviceToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ostatni czas aktywności użytkownika — aktualizowany przez heartbeat SignalR co 60 s.
    /// Null = użytkownik nigdy nie był online po zarejestrowaniu tokenu.
    /// Używane przez <see cref="BookingHub.Api.Services.FcmService"/> do detekcji offline.
    /// </summary>
    public DateTime? LastSeenAt { get; set; }

    public User? User { get; set; }
}
