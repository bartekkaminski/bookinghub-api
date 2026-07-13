using BookingHub.Api.Data;
using BookingHub.Api.Infrastructure;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Endpoint diagnostyczny — sprawdza stan kluczowych komponentów aplikacji.
/// Dostępny tylko dla uwierzytelnionych użytkowników.
/// </summary>
[ApiController]
[Route("api/diagnostics")]
[AllowAnonymous]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DiagnosticsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Zwraca raport diagnostyczny: Firebase, OutboxEvents, UserDeviceTokens.
    /// GET /api/diagnostics
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDiagnostics(CancellationToken ct)
    {
        // ── Firebase ─────────────────────────────────────────────────────────
        var firebaseInitialized = FirebaseApp.DefaultInstance is not null;
        var firebaseKeyPresent  = !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_KEY"));

        // ── OutboxEvents ─────────────────────────────────────────────────────
        var outboxPending   = await _db.OutboxEvents.CountAsync(e => !e.IsProcessed && e.RetryCount < 5, ct);
        var outboxFailed    = await _db.OutboxEvents.CountAsync(e => !e.IsProcessed && e.RetryCount >= 5, ct);
        var outboxProcessed = await _db.OutboxEvents.CountAsync(e => e.IsProcessed, ct);

        var recentEvents = await _db.OutboxEvents
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => new
            {
                e.Id,
                e.EventType,
                e.OrganizationId,
                e.IsProcessed,
                e.RetryCount,
                e.ErrorMessage,
                e.CreatedAt,
                e.ProcessedAt,
            })
            .ToListAsync(ct);

        // ── UserDeviceTokens ─────────────────────────────────────────────────
        var totalTokens = await _db.UserDeviceTokens.CountAsync(ct);

        var now = DateTime.UtcNow;
        var onlineThreshold = now.AddMinutes(-2);

        var onlineTokens  = await _db.UserDeviceTokens.CountAsync(t => t.LastSeenAt >= onlineThreshold, ct);
        var offlineTokens = await _db.UserDeviceTokens.CountAsync(t => t.LastSeenAt == null || t.LastSeenAt < onlineThreshold, ct);

        var recentTokens = await _db.UserDeviceTokens
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new
            {
                t.UserId,
                t.Platform,
                TokenPrefix = t.Token.Length > 20 ? t.Token.Substring(0, 20) + "…" : t.Token,
                t.LastSeenAt,
                t.CreatedAt,
                IsOnline = t.LastSeenAt != null && t.LastSeenAt >= onlineThreshold,
            })
            .ToListAsync(ct);

        return Ok(new
        {
            timestamp = now,
            firebase = new
            {
                initialized   = firebaseInitialized,
                envKeyPresent = firebaseKeyPresent,
                status        = firebaseInitialized ? "OK" : (firebaseKeyPresent ? "KEY_PRESENT_BUT_INIT_FAILED" : "NO_KEY"),
                initError     = FirebaseInitError.Message,
                rawKeyPrefix  = FirebaseInitError.RawKeyPrefix,
                base64Decoded = FirebaseInitError.Base64Decoded,
                jsonPrefix    = FirebaseInitError.JsonPrefix,
            },
            outbox = new
            {
                pending   = outboxPending,
                failed    = outboxFailed,
                processed = outboxProcessed,
                recent    = recentEvents,
            },
            deviceTokens = new
            {
                total   = totalTokens,
                online  = onlineTokens,
                offline = offlineTokens,
                recent  = recentTokens,
            },
        });
    }

    /// <summary>
    /// Usuwa zduplikowane tokeny — zostawia najnowszy dla każdej pary (userId, platform).
    /// POST /api/diagnostics/cleanup-tokens
    /// </summary>
    [HttpPost("cleanup-tokens")]
    public async Task<IActionResult> CleanupTokens(CancellationToken ct)
    {
        var duplicates = await db.UserDeviceTokens
            .GroupBy(t => new { t.UserId, t.Platform })
            .Where(g => g.Count() > 1)
            .ToListAsync(ct);

        int deleted = 0;
        foreach (var group in duplicates)
        {
            var toDelete = group
                .OrderByDescending(t => t.CreatedAt)
                .Skip(1)
                .ToList();

            db.UserDeviceTokens.RemoveRange(toDelete);
            deleted += toDelete.Count;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { deleted, message = $"Usunięto {deleted} zduplikowanych tokenów" });
    }

    [HttpPost("test-push/{tokenPrefix}")]
    public async Task<IActionResult> TestPush(string tokenPrefix, CancellationToken ct)
    {
        if (FirebaseApp.DefaultInstance is null)
            return BadRequest(new { error = "Firebase not initialized" });

        var token = await _db.UserDeviceTokens
            .Where(t => t.Token.StartsWith(tokenPrefix))
            .Select(t => t.Token)
            .FirstOrDefaultAsync(ct);

        if (token is null)
            return NotFound(new { error = $"No token starting with '{tokenPrefix}'" });

        try
        {
            var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = "BookingHub — test FCM",
                    Body  = $"Push dotarł o {DateTime.UtcNow:HH:mm:ss} UTC ✅",
                },
                Webpush = new WebpushConfig
                {
                    Notification = new WebpushNotification
                    {
                        Title = "BookingHub — test FCM",
                        Body  = $"Push dotarł o {DateTime.UtcNow:HH:mm:ss} UTC ✅",
                        Icon  = "/pwa-192x192.png",
                    },
                    FcmOptions = new WebpushFcmOptions { Link = "https://bookinghub-web.pages.dev/" },
                },
            }, ct);

            return Ok(new { sent = true, messageId, tokenPrefix = token.Substring(0, Math.Min(20, token.Length)) });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { sent = false, error = ex.Message });
        }
    }
}
