using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BookingHub.Api.Hubs;

/// <summary>
/// Centralny hub SignalR — real-time komunikacja z klientami.
///
/// Grupy:
///   org-{organizationId}  — wszyscy aktywni członkowie danej organizacji
///   user-{userId}         — bezpośrednie powiadomienia dla konkretnego użytkownika
///
/// Metody klienta → serwera:
///   JoinOrganization(orgId)  — jawne dołączenie do grupy org (po zmianie organizacji)
///   Heartbeat()              — sygnał aktywności (aktualizuje LastSeenAt tokenów FCM)
///
/// Zdarzenia serwera → klienta (wysyłane przez OutboxProcessor):
///   NewMessage      — nowa wiadomość
///   NewReply        — nowa odpowiedź
///   MessageDeleted  — usunięta wiadomość
/// </summary>
[Authorize]
public sealed class AppHub : Hub
{
    private readonly ICurrentUserService _currentUser;
    private readonly IOrganizationMemberRepository _memberRepo;
    private readonly IUserDeviceTokenRepository _deviceTokens;
    private readonly ILogger<AppHub> _logger;

    public AppHub(
        ICurrentUserService currentUser,
        IOrganizationMemberRepository memberRepo,
        IUserDeviceTokenRepository deviceTokens,
        ILogger<AppHub> logger)
    {
        _currentUser  = currentUser;
        _memberRepo   = memberRepo;
        _deviceTokens = deviceTokens;
        _logger       = logger;
    }

    /// <summary>
    /// Wywoływane przy nawiązaniu połączenia WebSocket.
    /// Dołącza klienta do grupy personal i wszystkich grup org, do których należy.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await JoinUserGroupsAsync(Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Wywoływane przy rozłączeniu. Grupy są automatycznie czyszczone przez SignalR.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
        {
            _logger.LogWarning(
                exception,
                "AppHub: rozłączenie z błędem. ConnectionId={Id}",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ── Metody klienta → serwera ──────────────────────────────────────────────

    /// <summary>
    /// Dołącza klienta do grupy danej organizacji.
    /// Weryfikuje aktywne członkostwo przed dołączeniem.
    /// </summary>
    public async Task JoinOrganization(string orgId)
    {
        if (!Guid.TryParse(orgId, out var organizationId))
        {
            _logger.LogWarning("AppHub.JoinOrganization: nieprawidłowy orgId={OrgId}", orgId);
            return;
        }

        var personId = _currentUser.PersonId;
        if (personId is null) return;

        var memberships = await _memberRepo.GetByPersonIdAsync(personId.Value, Context.ConnectionAborted);
        if (!memberships.Any(m => m.OrganizationId == organizationId && m.IsActive))
        {
            _logger.LogWarning(
                "AppHub.JoinOrganization: odmowa — userId={UserId} nie jest aktywnym członkiem org={OrgId}",
                _currentUser.UserId, organizationId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, OrgGroup(organizationId), Context.ConnectionAborted);
    }

    /// <summary>
    /// Sygnał aktywności — aktualizuje LastSeenAt wyłącznie dla konkretnego tokenu FCM urządzenia.
    /// Wywoływane przez klienta co 60 sekund wraz z jego tokenem FCM.
    ///
    /// Kluczowe: aktualizujemy TYLKO token tego urządzenia, nie wszystkie tokeny użytkownika.
    /// Dzięki temu desktop nie "ożywia" tokenu telefonu — FCM trafi na telefon gdy app jest
    /// zamknięta, nawet jeśli użytkownik ma otwartą sesję na komputerze.
    /// </summary>
    public async Task Heartbeat(string? fcmToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return;

        // Jeśli to urządzenie nie ma tokenu FCM (np. użytkownik nie włączył powiadomień),
        // heartbeat jest no-op — nie ma czego aktualizować.
        if (string.IsNullOrWhiteSpace(fcmToken)) return;

        await _deviceTokens.UpdateLastSeenByTokenAsync(userId.Value, fcmToken, DateTime.UtcNow, Context.ConnectionAborted);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task JoinUserGroupsAsync(CancellationToken ct)
    {
        var userId   = _currentUser.UserId;
        var personId = _currentUser.PersonId;

        if (userId is null || personId is null)
        {
            _logger.LogWarning(
                "AppHub.OnConnectedAsync: brak userId/personId. ConnectionId={Id}",
                Context.ConnectionId);
            return;
        }

        // Osobista grupa — na przyszłe powiadomienia kierowane bezpośrednio do użytkownika
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value), ct);

        // Grupy wszystkich aktywnych organizacji
        var memberships = await _memberRepo.GetByPersonIdAsync(personId.Value, ct);
        var activeOrgs  = memberships.Where(m => m.IsActive).Select(m => m.OrganizationId).ToList();

        foreach (var orgId in activeOrgs)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, OrgGroup(orgId), ct);
        }

        _logger.LogDebug(
            "AppHub: połączono userId={UserId}, dołączono do {Count} grup org. ConnectionId={Id}",
            userId, activeOrgs.Count, Context.ConnectionId);
    }

    // ── Statyczne pomocniki nazw grup (używane też przez OutboxProcessor) ─────

    public static string OrgGroup(Guid organizationId)  => $"org-{organizationId}";
    public static string UserGroup(Guid userId)          => $"user-{userId}";
}
