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
