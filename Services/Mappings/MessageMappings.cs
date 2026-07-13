using BookingHub.Api.Dtos.Message;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class MessageMappings
{
    private const int BodyPreviewLength = 120;

    public static MessageSummaryResponse ToSummary(this Message message, Guid viewerMemberId) => new()
    {
        Id                = message.Id,
        Subject           = message.Subject,
        BodyPreview       = message.Body.Length > BodyPreviewLength
            ? message.Body[..BodyPreviewLength] + "…"
            : message.Body,
        SentAt            = message.SentAt,
        SenderMemberId    = message.SenderMemberId,
        SenderName        = message.Sender?.ResolveDisplayName() ?? string.Empty,
        IsAutomatic       = message.IsAutomatic,
        IsRead            = message.Recipients.FirstOrDefault(r => r.RecipientMemberId == viewerMemberId)?.IsRead ?? false,
        RecipientsCount   = message.Recipients.Count,
        RepliesCount      = message.Replies.Count,
        RelatedEventId    = message.RelatedEventId,
        RelatedEventTitle = message.RelatedEvent?.Title,
        ParentMessageId   = message.ParentMessageId,
    };

    public static ConversationSummaryResponse ToConversationSummary(this Message message, Guid viewerMemberId)
    {
        var lastReply    = message.Replies.OrderByDescending(r => r.SentAt).FirstOrDefault();
        var initiatedByMe = message.SenderMemberId == viewerMemberId;

        var hasUnread = message.Recipients.Any(r => r.RecipientMemberId == viewerMemberId && !r.IsRead) ||
                        message.Replies.Any(r => r.Recipients.Any(rec => rec.RecipientMemberId == viewerMemberId && !rec.IsRead));

        var unreadCount = message.Recipients.Count(r => r.RecipientMemberId == viewerMemberId && !r.IsRead) +
                          message.Replies.Sum(r => r.Recipients.Count(rec => rec.RecipientMemberId == viewerMemberId && !rec.IsRead));

        string otherPartyName;
        Guid?  otherPartyMemberId;
        if (initiatedByMe)
        {
            var first = message.Recipients.FirstOrDefault();
            otherPartyName     = first?.Recipient?.ResolveDisplayName() ?? string.Empty;
            otherPartyMemberId = first?.RecipientMemberId;
        }
        else
        {
            otherPartyName     = message.Sender?.ResolveDisplayName() ?? string.Empty;
            otherPartyMemberId = message.SenderMemberId;
        }

        var rawPreview = lastReply?.Body ?? message.Body;
        var preview    = rawPreview.Length > BodyPreviewLength
            ? rawPreview[..BodyPreviewLength] + "…"
            : rawPreview;

        return new ConversationSummaryResponse
        {
            Id                  = message.Id,
            Subject             = message.Subject,
            SentAt              = message.SentAt,
            LastMessageAt       = lastReply?.SentAt ?? message.SentAt,
            LastMessagePreview  = preview,
            LastSenderName      = lastReply?.Sender?.ResolveDisplayName()
                                  ?? message.Sender?.ResolveDisplayName()
                                  ?? string.Empty,
            HasUnread           = hasUnread,
            UnreadCount         = unreadCount,
            RepliesCount        = message.Replies.Count,
            IsAutomatic         = message.IsAutomatic,
            InitiatedByMe       = initiatedByMe,
            RelatedEventId      = message.RelatedEventId,
            RelatedEventTitle   = message.RelatedEvent?.Title,
            OtherPartyName      = otherPartyName,
            OtherPartyMemberId  = otherPartyMemberId,
            ParticipantsCount   = message.Recipients.Count + 1,
        };
    }

    public static MessageDetailResponse ToDetail(this Message message) => new()
    {
        Id                = message.Id,
        OrganizationId    = message.OrganizationId,
        Subject           = message.Subject,
        Body              = message.Body,
        SentAt            = message.SentAt,
        SenderMemberId    = message.SenderMemberId,
        SenderName        = message.Sender?.ResolveDisplayName() ?? string.Empty,
        SenderPhotoUrl    = message.Sender?.PhotoUrl ?? message.Sender?.Person?.PhotoUrl,
        IsAutomatic       = message.IsAutomatic,
        RelatedEventId    = message.RelatedEventId,
        RelatedEventTitle = message.RelatedEvent?.Title,
        ParentMessageId   = message.ParentMessageId,
        Recipients        = message.Recipients.Select(r => new MessageRecipientInfo
        {
            MemberId    = r.RecipientMemberId,
            DisplayName = r.Recipient?.ResolveDisplayName() ?? string.Empty,
            PhotoUrl    = r.Recipient?.PhotoUrl ?? r.Recipient?.Person?.PhotoUrl,
            IsRead      = r.IsRead,
            ReadAt      = r.ReadAt,
        }).ToList(),
        Replies           = message.Replies.Select(r => r.ToSummary(Guid.Empty)).ToList(),
    };
}
