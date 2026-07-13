namespace BookingHub.Api.Infrastructure;

/// <summary>
/// Przechowuje błąd inicjalizacji Firebase Admin SDK do wyświetlenia w /api/diagnostics.
/// </summary>
public static class FirebaseInitError
{
    public static string? Message { get; set; }
}
