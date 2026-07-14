using System.Text.Json;
using BookingHub.Api.Data;
using BookingHub.Api.Hubs;
using BookingHub.Api.Models;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Services;

/// <summary>
/// BackgroundService — przetwarza zdarzenia z OutboxEvent i wysyła je przez SignalR i FCM.
///
/// Algorytm:
///   1. Co <see cref="PollInterval"/> sprawdza niepzetworzone zdarzenia (IsProcessed=false, RetryCount &lt; 5).
///   2. Dla każdego zdarzenia:
///      a. Wysyła do grupy SignalR org-{organizationId} (zawsze).
///      b. Dla zdarzeń wiadomości: wysyła FCM do offline-odbiorców.
///   3. Co <see cref="CleanupInterval"/> usuwa zdarzenia starsze niż <see cref="RetentionPeriod"/>.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private static readonly TimeSpan PollInterval     = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan CleanupInterval  = TimeSpan.FromHours(24);
    private static readonly TimeSpan RetentionPeriod  = TimeSpan.FromDays(7);
    private const int MaxRetries = 5;
    private const int BatchSize  = 50;

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<AppHub>  _hubContext;
    private readonly IFcmService          _fcmService;
    private readonly ILogger<OutboxProcessor> _logger;

    private DateTime _lastCleanup = DateTime.MinValue;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IHubContext<AppHub> hubContext,
        IFcmService fcmService,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext   = hubContext;
        _fcmService   = fcmService;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor: uruchomiony.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);

                if (DateTime.UtcNow - _lastCleanup > CleanupInterval)
                {
                    await CleanupOldEventsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor: nieoczekiwany błąd w pętli głównej.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("OutboxProcessor: zatrzymany.");
    }

    // ── Processing ───────────────────────────────────────────────────────────

    private async Task ProcessPendingEventsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var events = await db.OutboxEvents
            .Where(e => !e.IsProcessed && e.RetryCount < MaxRetries)
            .OrderBy(e => e.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (events.Count == 0) return;

        foreach (var evt in events)
        {
            try
            {
                await DispatchEventAsync(evt, ct);
                evt.IsProcessed  = true;
                evt.ProcessedAt  = DateTime.UtcNow;
                evt.ErrorMessage = null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                evt.RetryCount++;
                evt.ErrorMessage = ex.Message.Length > 2000
                    ? ex.Message[..2000]
                    : ex.Message;

                _logger.LogWarning(
                    ex,
                    "OutboxProcessor: błąd zdarzenia {Id} (próba {Retry}/{Max}). EventType={Type}",
                    evt.Id, evt.RetryCount, MaxRetries, evt.EventType);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task DispatchEventAsync(OutboxEvent evt, CancellationToken ct)
    {
        var groupName = AppHub.OrgGroup(evt.OrganizationId);

        switch (evt.EventType)
        {
            case HubEvents.NewMessage:
            {
                var payload = Deserialize<NewMessagePayload>(evt.PayloadJson);
                if (payload is null) return;

                // SignalR — do wszystkich online-członków organizacji
                await _hubContext.Clients
                    .Group(groupName)
                    .SendAsync(HubEvents.NewMessage, payload, ct);

                // FCM — do offline-odbiorców
                await _fcmService.SendToOfflineMembersAsync(
                    payload.RecipientMemberIds,
                    title: payload.SenderName,
                    body:  BuildNotificationBody(payload.Subject, payload.Preview),
                    data: new Dictionary<string, string>
                    {
                        ["eventType"]      = HubEvents.NewMessage,
                        ["messageId"]      = payload.MessageId.ToString(),
                        ["organizationId"] = payload.OrganizationId.ToString(),
                        ["actionUrl"]      = $"/app/org/{payload.OrganizationId}/messages/{payload.MessageId}",
                        ["subject"]        = payload.Subject,
                    },
                    ct);
                break;
            }

            case HubEvents.NewReply:
            {
                var payload = Deserialize<NewReplyPayload>(evt.PayloadJson);
                if (payload is null) return;

                await _hubContext.Clients
                    .Group(groupName)
                    .SendAsync(HubEvents.NewReply, payload, ct);

                await _fcmService.SendToOfflineMembersAsync(
                    payload.RecipientMemberIds,
                    title: payload.SenderName,
                    body:  BuildNotificationBody(payload.Subject, payload.Preview),
                    data: new Dictionary<string, string>
                    {
                        ["eventType"]      = HubEvents.NewReply,
                        ["messageId"]      = payload.MessageId.ToString(),
                        ["conversationId"] = payload.ConversationId.ToString(),
                        ["organizationId"] = payload.OrganizationId.ToString(),
                        ["actionUrl"]      = $"/app/org/{payload.OrganizationId}/messages/{payload.ConversationId}",
                        ["subject"]        = payload.Subject,
                    },
                    ct);
                break;
            }

            case HubEvents.MessageDeleted:
            {
                var payload = Deserialize<MessageDeletedPayload>(evt.PayloadJson);
                if (payload is null) return;

                await _hubContext.Clients
                    .Group(groupName)
                    .SendAsync(HubEvents.MessageDeleted, payload, ct);
                // Brak FCM dla usunięcia — zmiana już widoczna przy następnym otwarciu
                break;
            }

            default:
                _logger.LogWarning(
                    "OutboxProcessor: nieznany EventType={Type} dla zdarzenia {Id}",
                    evt.EventType, evt.Id);
                break;
        }
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────

    private async Task CleanupOldEventsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var threshold = DateTime.UtcNow.Subtract(RetentionPeriod);
            var deleted   = await db.OutboxEvents
                .Where(e => e.IsProcessed && e.ProcessedAt < threshold)
                .ExecuteDeleteAsync(ct);

            if (deleted > 0)
                _logger.LogInformation("OutboxProcessor: usunięto {Count} starych zdarzeń.", deleted);

            _lastCleanup = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OutboxProcessor: błąd podczas czyszczenia starych zdarzeń.");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static T? Deserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, DeserializeOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Buduje treść powiadomienia: "Temat — fragment wiadomości" lub sam temat gdy brak podglądu.
    /// </summary>
    private static string BuildNotificationBody(string subject, string preview)
    {
        if (string.IsNullOrWhiteSpace(preview)) return subject;
        return $"{subject} — {preview}";
    }
}
