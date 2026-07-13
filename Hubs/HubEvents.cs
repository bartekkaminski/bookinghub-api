namespace BookingHub.Api.Hubs;

/// <summary>
/// Stałe nazw zdarzeń wysyłanych przez SignalR do klientów
/// i przechowywanych jako EventType w OutboxEvent.
/// Muszą być identyczne z nazwami obsługiwanymi na frontendzie (camelCase w JSON).
/// </summary>
public static class HubEvents
{
    public const string NewMessage     = "NewMessage";
    public const string NewReply       = "NewReply";
    public const string MessageDeleted = "MessageDeleted";
}
