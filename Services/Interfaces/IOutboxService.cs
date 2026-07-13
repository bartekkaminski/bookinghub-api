namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Transactional Outbox — rejestruje zdarzenia do wysłania przez SignalR / FCM.
///
/// Kluczowa właściwość: <see cref="Enqueue"/> NIE wywołuje SaveChangesAsync.
/// Zdarzenie jest dodawane do change-trackera i zapisywane atomicznie
/// razem z operacją biznesową (np. nową wiadomością) przez SaveChangesAsync
/// wywołane wewnątrz repozytorium — gwarantuje transakcyjną spójność.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Dodaje zdarzenie do outboxa w ramach bieżącej transakcji.
    /// Nie wywołuje SaveChangesAsync — wywołujące repozytorium zrobi to automatycznie.
    /// </summary>
    /// <param name="organizationId">Organizacja (wyznacza grupę SignalR).</param>
    /// <param name="eventType">Typ zdarzenia — stała z <see cref="BookingHub.Api.Hubs.HubEvents"/>.</param>
    /// <param name="payload">Payload — zostanie zserializowany do JSON.</param>
    void Enqueue(Guid organizationId, string eventType, object payload);
}
