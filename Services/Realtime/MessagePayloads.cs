namespace BookingHub.Api.Services.Realtime;

/// <summary>Payload zdarzenia nowej wiadomości (nowy wątek).</summary>
public sealed record NewMessagePayload(
    Guid MessageId,
    Guid OrganizationId,
    Guid SenderMemberId,
    IReadOnlyList<Guid> RecipientMemberIds,
    string Subject
);

/// <summary>Payload zdarzenia nowej odpowiedzi w istniejącym wątku.</summary>
public sealed record NewReplyPayload(
    Guid MessageId,
    Guid ConversationId,
    Guid OrganizationId,
    Guid SenderMemberId,
    IReadOnlyList<Guid> RecipientMemberIds,
    string Subject
);

/// <summary>Payload zdarzenia usunięcia wiadomości.</summary>
public sealed record MessageDeletedPayload(
    Guid MessageId,
    Guid OrganizationId
);
