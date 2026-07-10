using BookingHub.Api.Dtos.Message;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis wiadomości wewnętrznych — skrzynka odbiorcza, nadawcza, odpowiedzi.
/// </summary>
public interface IMessageService
{
    /// <summary>Pobiera stronicowaną skrzynkę odbiorczą dla danego członka.</summary>
    Task<PagedResult<MessageSummaryResponse>> GetInboxAsync(Guid memberId, MessageFilterParams filter, CancellationToken ct = default);

    /// <summary>Pobiera stronicowaną skrzynkę nadawczą dla danego członka.</summary>
    Task<PagedResult<MessageSummaryResponse>> GetOutboxAsync(Guid memberId, MessageFilterParams filter, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły wiadomości wraz z odbiorcami i odpowiedziami.</summary>
    Task<MessageDetailResponse> GetByIdAsync(Guid messageId, Guid requestingMemberId, CancellationToken ct = default);

    /// <summary>Wysyła nową wiadomość do wskazanych odbiorców.</summary>
    Task<MessageDetailResponse> SendAsync(Guid organizationId, Guid senderMemberId, SendMessageRequest request, CancellationToken ct = default);

    /// <summary>
    /// Wysyła automatyczną wiadomość systemową (np. powiadomienie o odwołaniu zajęć).
    /// Nadawca = system / organizacja.
    /// </summary>
    Task SendSystemMessageAsync(Guid organizationId, string subject, string body, IReadOnlyList<Guid> recipientMemberIds, Guid? relatedEventId = null, CancellationToken ct = default);

    /// <summary>Odpowiada na wiadomość — wysyła odpowiedź do nadawcy + ew. do innych odbiorców.</summary>
    Task<MessageDetailResponse> ReplyAsync(Guid parentMessageId, Guid senderMemberId, ReplyMessageRequest request, CancellationToken ct = default);

    /// <summary>Oznacza wiadomość jako przeczytaną dla danego odbiorcy.</summary>
    Task MarkAsReadAsync(Guid messageId, Guid memberId, CancellationToken ct = default);

    /// <summary>Oznacza wszystkie nieprzeczytane wiadomości jako przeczytane.</summary>
    Task MarkAllAsReadAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>Zwraca liczbę nieprzeczytanych wiadomości dla danego członka.</summary>
    Task<int> GetUnreadCountAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>Usuwa wiadomość (soft delete) — tylko dla nadawcy lub admina.</summary>
    Task DeleteAsync(Guid messageId, Guid requestingMemberId, CancellationToken ct = default);
}
