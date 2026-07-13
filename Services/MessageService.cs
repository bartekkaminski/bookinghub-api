using BookingHub.Api.Dtos.Message;
using BookingHub.Api.Hubs;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;
using BookingHub.Api.Services.Realtime;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis wiadomości wewnętrznych — skrzynka odbiorcza, nadawcza, odpowiedzi.
/// </summary>
public sealed class MessageService : IMessageService
{
    private readonly IMessageRepository _messages;
    private readonly IOrganizationMemberRepository _members;
    private readonly IOutboxService _outbox;
    private readonly ILogger<MessageService> _logger;

    public MessageService(
        IMessageRepository messages,
        IOrganizationMemberRepository members,
        IOutboxService outbox,
        ILogger<MessageService> logger)
    {
        _messages = messages;
        _members  = members;
        _outbox   = outbox;
        _logger   = logger;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<ConversationSummaryResponse>> GetConversationsAsync(Guid memberId, MessageFilterParams filter, CancellationToken ct = default)
    {
        var paged = await _messages.GetConversationsAsync(memberId, filter.Page, filter.PageSize, ct);
        return paged.Map(m => m.ToConversationSummary(memberId));
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MessageSummaryResponse>> GetInboxAsync(Guid memberId, MessageFilterParams filter, CancellationToken ct = default)
    {
        var paged = await _messages.GetInboxAsync(memberId, filter.Page, filter.PageSize, false, ct);
        return paged.Map(m => m.ToSummary(memberId));
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MessageSummaryResponse>> GetOutboxAsync(Guid memberId, MessageFilterParams filter, CancellationToken ct = default)
    {
        var paged = await _messages.GetOutboxAsync(memberId, filter.Page, filter.PageSize, ct);
        return paged.Map(m => m.ToSummary(memberId));
    }

    /// <inheritdoc/>
    public async Task<MessageDetailResponse> GetByIdAsync(Guid messageId, Guid requestingMemberId, CancellationToken ct = default)
    {
        var message = await _messages.GetWithDetailsAsync(messageId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Wiadomość {messageId} nie istnieje.");

        // Tylko nadawca lub odbiorca może odczytać wiadomość.
        var isSender    = message.SenderMemberId == requestingMemberId;
        var isRecipient = message.Recipients.Any(r => r.RecipientMemberId == requestingMemberId);
        if (!isSender && !isRecipient)
            throw new ServiceException(ServiceErrorCode.Forbidden,
                "Nie masz dostępu do tej wiadomości.");

        // Automatycznie oznacz jako przeczytaną przy otwarciu (tylko odbiorca).
        var recipientEntry = message.Recipients.FirstOrDefault(r => r.RecipientMemberId == requestingMemberId);
        if (recipientEntry is { IsRead: false })
        {
            recipientEntry.IsRead = true;
            recipientEntry.ReadAt = DateTime.UtcNow;
            await _messages.MarkAsReadAsync(messageId, requestingMemberId, ct);
        }

        // Oznacz też odpowiedzi w tym wątku jako przeczytane (są oddzielnymi rekordami Message,
        // ale użytkownik je widzi otwierając wątek — nie powinny zawyżać licznika).
        foreach (var reply in message.Replies)
        {
            await _messages.MarkAsReadAsync(reply.Id, requestingMemberId, ct);
        }

        return message.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<MessageDetailResponse> SendAsync(Guid organizationId, Guid senderMemberId, SendMessageRequest request, CancellationToken ct = default)
    {
        if (!request.RecipientMemberIds.Any())
            throw new ServiceException(ServiceErrorCode.MessageNoRecipients,
                "Lista odbiorców jest pusta.", nameof(request.RecipientMemberIds));

        // Wszyscy odbiorcy muszą należeć do tej samej organizacji — ochrona przed wysyłaniem do innych tenantów.
        var recipientIds = request.RecipientMemberIds.Distinct().ToList();
        var orgMembers   = await _members.GetByOrganizationAsync(organizationId, ct);
        var orgMemberIds = orgMembers.Select(m => m.Id).ToHashSet();

        var invalidIds = recipientIds.Where(id => !orgMemberIds.Contains(id)).ToList();
        if (invalidIds.Count > 0)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                $"Odbiorcy {string.Join(", ", invalidIds)} nie należą do tej organizacji.",
                nameof(request.RecipientMemberIds));

        var message = new Message
        {
            // Jawne Guid.NewGuid() — potrzebne do zbudowania payloadu outboxa PRZED SaveChangesAsync
            Id             = Guid.NewGuid(),
            OrganizationId = organizationId,
            SenderMemberId = senderMemberId,
            Subject        = request.Subject.Trim(),
            Body           = request.Body.Trim(),
            SentAt         = DateTime.UtcNow,
            IsAutomatic    = false,
            RelatedEventId = request.RelatedEventId,
            Recipients     = recipientIds.Select(memberId => new MessageRecipient
            {
                RecipientMemberId = memberId,
                IsRead            = false,
            }).ToList(),
        };

        // Enqueue PRZED AddAsync — oba zostaną zapisane atomicznie przez SaveChangesAsync w AddAsync
        _outbox.Enqueue(organizationId, HubEvents.NewMessage, new NewMessagePayload(
            MessageId:          message.Id,
            OrganizationId:     organizationId,
            SenderMemberId:     senderMemberId,
            RecipientMemberIds: recipientIds,
            Subject:            message.Subject
        ));

        var created = await _messages.AddAsync(message, ct);
        var details = await _messages.GetWithDetailsAsync(created.Id, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task SendSystemMessageAsync(Guid organizationId, string subject, string body,
        IReadOnlyList<Guid> recipientMemberIds, Guid? relatedEventId = null, CancellationToken ct = default)
    {
        if (!recipientMemberIds.Any())
        {
            _logger.LogWarning("SendSystemMessage: Brak odbiorców dla '{Subject}'.", subject);
            return;
        }

        // Znajdź system/pierwszy admin member w organizacji jako nadawca
        var adminMembers = await _members.GetByRoleAsync(organizationId, MemberRole.Admin, ct);
        var systemSender = adminMembers.FirstOrDefault();
        if (systemSender is null)
        {
            _logger.LogWarning("SendSystemMessage: Brak admina w organizacji {OrgId} — pomijam wiadomość.", organizationId);
            return;
        }

        var message = new Message
        {
            OrganizationId = organizationId,
            SenderMemberId = systemSender.Id,
            Subject        = subject,
            Body           = body,
            SentAt         = DateTime.UtcNow,
            IsAutomatic    = true,
            RelatedEventId = relatedEventId,
            Recipients     = recipientMemberIds.Distinct().Select(memberId => new MessageRecipient
            {
                RecipientMemberId = memberId,
                IsRead            = false,
            }).ToList(),
        };

        await _messages.AddAsync(message, ct);
        _logger.LogInformation("Wysłano systemową wiadomość '{Subject}' do {Count} odbiorców.",
            subject, recipientMemberIds.Count);
    }

    /// <inheritdoc/>
    public async Task<MessageDetailResponse> ReplyAsync(Guid parentMessageId, Guid senderMemberId, ReplyMessageRequest request, CancellationToken ct = default)
    {
        var parent = await _messages.GetWithDetailsAsync(parentMessageId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Wiadomość {parentMessageId} nie istnieje.");

        // Tylko nadawca lub odbiorca oryginalnej wiadomości może odpowiedzieć.
        var isSender    = parent.SenderMemberId == senderMemberId;
        var isRecipient = parent.Recipients.Any(r => r.RecipientMemberId == senderMemberId);
        if (!isSender && !isRecipient)
            throw new ServiceException(ServiceErrorCode.Forbidden,
                "Możesz odpowiadać tylko na wiadomości, do których masz dostęp.");

        // Odpowiedź wysyłamy do nadawcy wiadomości nadrzędnej
        var replyRecipients = new List<Guid> { parent.SenderMemberId };

        // Oraz do wszystkich innych odbiorców (poza aktualnym nadawcą)
        replyRecipients.AddRange(
            parent.Recipients
                .Select(r => r.RecipientMemberId)
                .Where(id => id != senderMemberId));

        var replyRecipientList = replyRecipients.Distinct().ToList();

        var reply = new Message
        {
            // Jawne Guid.NewGuid() — potrzebne do zbudowania payloadu outboxa PRZED SaveChangesAsync
            Id             = Guid.NewGuid(),
            OrganizationId = parent.OrganizationId,
            SenderMemberId = senderMemberId,
            Subject        = parent.Subject.StartsWith("Re: ", StringComparison.OrdinalIgnoreCase)
                ? parent.Subject
                : $"Re: {parent.Subject}",
            Body            = request.Body.Trim(),
            SentAt          = DateTime.UtcNow,
            IsAutomatic     = false,
            ParentMessageId = parentMessageId,
            RelatedEventId  = parent.RelatedEventId,
            Recipients      = replyRecipientList.Select(memberId => new MessageRecipient
            {
                RecipientMemberId = memberId,
                IsRead            = false,
            }).ToList(),
        };

        // Enqueue PRZED AddAsync — atomiczność zapewniona przez SaveChangesAsync w AddAsync
        _outbox.Enqueue(parent.OrganizationId, HubEvents.NewReply, new NewReplyPayload(
            MessageId:          reply.Id,
            ConversationId:     parentMessageId,
            OrganizationId:     parent.OrganizationId,
            SenderMemberId:     senderMemberId,
            RecipientMemberIds: replyRecipientList,
            Subject:            reply.Subject
        ));

        var created = await _messages.AddAsync(reply, ct);
        var details = await _messages.GetWithDetailsAsync(created.Id, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task MarkAsReadAsync(Guid messageId, Guid memberId, CancellationToken ct = default)
    {
        await _messages.MarkAsReadAsync(messageId, memberId, ct);
    }

    /// <inheritdoc/>
    public async Task MarkAllAsReadAsync(Guid memberId, CancellationToken ct = default)
    {
        await _messages.MarkAllAsReadAsync(memberId, ct);
    }

    /// <inheritdoc/>
    public async Task<int> GetUnreadCountAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _messages.GetUnreadCountAsync(memberId, ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid messageId, Guid requestingMemberId, CancellationToken ct = default)
    {
        var message = await _messages.GetWithDetailsAsync(messageId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Wiadomość {messageId} nie istnieje.");

        // Tylko nadawca może usunąć wiadomość.
        if (message.SenderMemberId != requestingMemberId)
            throw new ServiceException(ServiceErrorCode.Forbidden,
                "Możesz usuwać tylko wiadomości wysłane przez siebie.");

        // Enqueue PRZED DeleteAsync — atomiczność zapewniona przez SaveChangesAsync w DeleteAsync
        _outbox.Enqueue(message.OrganizationId, HubEvents.MessageDeleted, new MessageDeletedPayload(
            MessageId:      messageId,
            OrganizationId: message.OrganizationId
        ));

        await _messages.DeleteAsync(messageId, ct);
    }
}
