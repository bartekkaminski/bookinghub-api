namespace BookingHub.Api.Models;

/// <summary>Token FCM urządzenia użytkownika do wysyłania push notifications.</summary>
public class UserDeviceToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
