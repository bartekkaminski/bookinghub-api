namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis Firebase Cloud Messaging — wysyła push notifications do offline-użytkowników.
/// Rejestrowany jako Singleton — bezpieczny dla wielowątkowego dostępu z OutboxProcessor.
/// </summary>
public interface IFcmService
{
    /// <summary>
    /// Wysyła powiadomienie push do użytkowników powiązanych z podanymi memberIds,
    /// którzy są offline (LastSeenAt &lt; 2 minuty temu lub brak tokenu).
    /// </summary>
    Task SendToOfflineMembersAsync(
        IEnumerable<Guid> memberIds,
        string title,
        string body,
        Dictionary<string, string> data,
        CancellationToken ct = default);
}
