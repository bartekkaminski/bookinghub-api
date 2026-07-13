namespace BookingHub.Api.Infrastructure;

/// <summary>
/// Przechowuje diagnostykę inicjalizacji Firebase Admin SDK.
/// </summary>
public static class FirebaseInitError
{
    public static string? Message       { get; set; }
    public static string? RawKeyPrefix  { get; set; }  // pierwsze 30 znaków raw value
    public static bool?   Base64Decoded { get; set; }  // czy Base64 decode się powiódł
    public static string? JsonPrefix    { get; set; }  // pierwsze 30 znaków po decode
}
