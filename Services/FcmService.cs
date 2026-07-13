using BookingHub.Api.Data;
using BookingHub.Api.Services.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Services;

/// <summary>
/// Implementacja FCM — wysyła push notifications przez Firebase Admin SDK.
/// Singleton — dzieli instancję FirebaseApp między wszystkimi żądaniami.
/// </summary>
public sealed class FcmService : IFcmService
{
    /// <summary>FCM API ma limit 500 tokenów w jednej operacji SendEachAsync.</summary>
    private const int FcmBatchSize = 500;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FcmService> _logger;
    private readonly string _frontendBaseUrl;

    public FcmService(IServiceScopeFactory scopeFactory, ILogger<FcmService> logger, IConfiguration configuration)
    {
        _scopeFactory    = scopeFactory;
        _logger          = logger;
        // Pobierz pierwszą skonfigurowaną domenę frontendu jako bazowy URL do linków FCM
        var corsOrigins  = configuration["Cors__Origins"] ?? "";
        _frontendBaseUrl = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(o => o.Trim())
                               .FirstOrDefault(o => o.StartsWith("https://")) 
                           ?? "https://bookinghub-web.pages.dev";
    }

    /// <summary>
    /// Zwraca pełny HTTPS URL dla linku FCM — FCM odrzuca relatywne ścieżki.
    /// </summary>
    private string ToAbsoluteLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return _frontendBaseUrl + "/";
        if (url.StartsWith("https://") || url.StartsWith("http://")) return url;
        return _frontendBaseUrl.TrimEnd('/') + (url.StartsWith('/') ? url : "/" + url);
    }

    public async Task SendToOfflineMembersAsync(
        IEnumerable<Guid> memberIds,
        string title,
        string body,
        Dictionary<string, string> data,
        CancellationToken ct = default)
    {
        // Graceful degradation — Firebase może być nieskonfigurowany w dev/testach
        if (FirebaseApp.DefaultInstance is null)
        {
            _logger.LogDebug("FcmService: Firebase nie jest skonfigurowany — pomijanie.");
            return;
        }

        var memberIdList = memberIds.Distinct().ToList();
        if (memberIdList.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Resolver: memberIds → PersonIds → UserIds → wszystkie DeviceTokens
        // Celowo NIE filtrujemy po LastSeenAt — wysyłamy FCM do wszystkich tokenów odbiorców.
        // Jeśli aplikacja jest otwarta: SDK po stronie klienta (onMessage) obsłuży to cicho.
        // Jeśli aplikacja jest zamknięta: service worker pokaże powiadomienie.
        // Filtrowanie "offline" powodowałoby opóźnienie ~2 min po zamknięciu aplikacji.
        var personIds = await db.OrganizationMembers
            .Where(om => memberIdList.Contains(om.Id))
            .Select(om => om.PersonId)
            .ToListAsync(ct);

        if (personIds.Count == 0) return;

        var userIds = await db.Persons
            .Where(p => personIds.Contains(p.Id) && p.UserId.HasValue)
            .Select(p => p.UserId!.Value)
            .ToListAsync(ct);

        if (userIds.Count == 0) return;

        var allTokens = await db.UserDeviceTokens
            .Where(t => userIds.Contains(t.UserId))
            .Select(t => t.Token)
            .ToListAsync(ct);

        if (allTokens.Count == 0) return;

        _logger.LogDebug("FcmService: wysyłam FCM do {Count} tokenów.", allTokens.Count);
        await SendBatchAsync(allTokens, title, body, data, db, ct);
    }

    // ── Prywatne ─────────────────────────────────────────────────────────────

    private async Task SendBatchAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        Dictionary<string, string> data,
        AppDbContext db,
        CancellationToken ct)
    {
        var messaging = FirebaseMessaging.DefaultInstance;

        foreach (var batch in tokens.Chunk(FcmBatchSize))
        {
            // Suppress CS0618: Message.Token is marked [Obsolete("Use Fid")] in FirebaseAdmin 3.6+,
            // ale FCM registration token (nie FID) jest nadal wymagany do push notification.
#pragma warning disable CS0618
            var messages = batch.Select(token => new Message
            {
                Token        = token,
                Notification = new Notification { Title = title, Body = body },
                Data         = data,
                Webpush      = new WebpushConfig
                {
                    FcmOptions = new WebpushFcmOptions
                    {
                        Link = ToAbsoluteLink(data.GetValueOrDefault("actionUrl")),
                    },
                    Notification = new WebpushNotification
                    {
                        Icon  = "/pwa-192x192.png",
                        Badge = "/pwa-64x64.png",
                    },
                },
            }).ToList();
#pragma warning restore CS0618

            try
            {
                var response = await messaging.SendEachAsync(messages, ct);

                // Usuń zdezaktualizowane / nieznane tokeny (FCM reject)
                var invalidTokens = new List<string>();
                for (var i = 0; i < response.Responses.Count; i++)
                {
                    var resp = response.Responses[i];
                    if (resp.IsSuccess) continue;

                    var errorCode = resp.Exception?.MessagingErrorCode;
                    if (errorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                    {
                        invalidTokens.Add(batch[i]);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "FcmService: błąd tokenu [{Token}…]: {Error}",
                            batch[i][..Math.Min(20, batch[i].Length)],
                            resp.Exception?.Message);
                    }
                }

                if (invalidTokens.Count > 0)
                {
                    await RemoveInvalidTokensAsync(invalidTokens, db, ct);
                }

                _logger.LogDebug(
                    "FcmService batch: {Success}/{Total} sukces, {Invalid} nieprawidłowych.",
                    response.SuccessCount, response.Responses.Count, invalidTokens.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "FcmService: błąd SendEachAsync batch.");
            }
        }
    }

    private static async Task RemoveInvalidTokensAsync(
        IReadOnlyList<string> tokens,
        AppDbContext db,
        CancellationToken ct)
    {
        await db.UserDeviceTokens
            .Where(t => tokens.Contains(t.Token))
            .ExecuteDeleteAsync(ct);
    }
}
