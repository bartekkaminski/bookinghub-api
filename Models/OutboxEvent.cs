namespace BookingHub.Api.Models;

/// <summary>
/// Transactional Outbox — zdarzenie do wysłania przez SignalR lub FCM.
/// Zapisywane atomicznie razem z operacją biznesową (np. nową wiadomością)
/// przez wywołanie <see cref="BookingHub.Api.Services.OutboxService.Enqueue"/>.
/// Przetwarzane przez <see cref="BookingHub.Api.Services.OutboxProcessor"/>.
/// </summary>
public sealed class OutboxEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Organizacja, do której należy zdarzenie — wyznacza grupę SignalR.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Typ zdarzenia, np. "NewMessage", "NewReply", "MessageDeleted".</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Payload serializowany jako JSON.</summary>
    public string PayloadJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsProcessed { get; set; } = false;

    public DateTime? ProcessedAt { get; set; }

    /// <summary>Ostatni błąd przetwarzania (truncated do 2000 znaków).</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Liczba prób przetwarzania.
    /// Po osiągnięciu limitu (5) zdarzenie jest trwale pomijane.
    /// </summary>
    public int RetryCount { get; set; } = 0;
}
