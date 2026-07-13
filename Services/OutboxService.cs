using System.Text.Json;
using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Services.Interfaces;

namespace BookingHub.Api.Services;

/// <summary>
/// Implementacja outboxa — dodaje zdarzenia do DbContext bez wywołania SaveChangesAsync.
/// Atomiczność gwarantuje to, że wywołujące repozytorium (np. MessageRepository.AddAsync)
/// wywoła SaveChangesAsync na tym samym Scoped AppDbContext, zapisując oba obiekty razem.
/// </summary>
public sealed class OutboxService : IOutboxService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly AppDbContext _context;

    public OutboxService(AppDbContext context)
    {
        _context = context;
    }

    public void Enqueue(Guid organizationId, string eventType, object payload)
    {
        _context.OutboxEvents.Add(new OutboxEvent
        {
            OrganizationId = organizationId,
            EventType      = eventType,
            PayloadJson    = JsonSerializer.Serialize(payload, SerializerOptions),
        });
    }
}
