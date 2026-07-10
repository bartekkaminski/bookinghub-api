using System.ComponentModel.DataAnnotations;
using BookingHub.Api.Models;

namespace BookingHub.Api.Dtos.DeviceToken;

/// <summary>Żądanie rejestracji tokenu urządzenia FCM.</summary>
public sealed class RegisterDeviceTokenRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public DevicePlatform Platform { get; set; }
}

/// <summary>Odpowiedź z danymi zarejestrowanego tokenu urządzenia.</summary>
public sealed class DeviceTokenResponse
{
    public Guid Id { get; set; }
    public DevicePlatform Platform { get; set; }
    public DateTime CreatedAt { get; set; }
}
