using System.ComponentModel.DataAnnotations;

namespace BookingHub.Api.Dtos.Message;

/// <summary>Skrócone dane wiadomości — do listy inbox/outbox.</summary>
public sealed class MessageSummaryResponse
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public Guid SenderMemberId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public bool IsAutomatic { get; set; }
    public bool IsRead { get; set; }
    public int RecipientsCount { get; set; }
    public int RepliesCount { get; set; }
    public Guid? RelatedEventId { get; set; }
    public string? RelatedEventTitle { get; set; }
    public Guid? ParentMessageId { get; set; }
}

/// <summary>Pełne dane wiadomości — widok szczegółowy z odbiorcami i odpowiedziami.</summary>
public sealed class MessageDetailResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public Guid SenderMemberId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderPhotoUrl { get; set; }
    public bool IsAutomatic { get; set; }
    public Guid? RelatedEventId { get; set; }
    public string? RelatedEventTitle { get; set; }
    public Guid? ParentMessageId { get; set; }
    public IReadOnlyList<MessageRecipientInfo> Recipients { get; set; } = [];
    public IReadOnlyList<MessageSummaryResponse> Replies { get; set; } = [];
}

public sealed class MessageRecipientInfo
{
    public Guid MemberId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}

/// <summary>Żądanie wysłania nowej wiadomości.</summary>
public sealed class SendMessageRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Body { get; set; } = string.Empty;

    /// <summary>Lista Id członków organizacji (OrganizationMember), do których wysyłamy.</summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<Guid> RecipientMemberIds { get; set; } = [];

    /// <summary>Opcjonalne powiązanie z zajęciami.</summary>
    public Guid? RelatedEventId { get; set; }
}

/// <summary>Żądanie odpowiedzi na wiadomość.</summary>
public sealed class ReplyMessageRequest
{
    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Body { get; set; } = string.Empty;
}

/// <summary>Liczba nieprzeczytanych wiadomości.</summary>
public sealed class UnreadCountResponse
{
    public int UnreadCount { get; set; }
}
