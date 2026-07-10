using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium wiadomości wewnętrznych (Message + MessageRecipient).
/// </summary>
public interface IMessageRepository : IBaseRepository<Message>
{
    /// <summary>
    /// Pobiera wiadomość z pełnymi danymi — Sender, Recipients (+ OrganizationMember → Person), RelatedEvent, Replies.
    /// </summary>
    Task<Message?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wiadomość wraz z odpowiedziami (Replies, 1 poziom głębokości).
    /// </summary>
    Task<Message?> GetWithRepliesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę wiadomości z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<Message>> GetPagedAsync(MessageFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera skrzynkę odbiorczą (inbox) danego członka — wiadomości dla niego z metadanymi odczytu.
    /// </summary>
    Task<PagedResult<Message>> GetInboxAsync(Guid recipientMemberId, int page = 1, int pageSize = 20, bool onlyUnread = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wiadomości wysłane przez danego członka (outbox).
    /// </summary>
    Task<PagedResult<Message>> GetOutboxAsync(Guid senderMemberId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wiadomości powiązane z konkretnymi zajęciami.
    /// </summary>
    Task<IReadOnlyList<Message>> GetByRelatedEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca liczbę nieprzeczytanych wiadomości danego członka.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid recipientMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Oznacza wiadomość jako przeczytaną przez odbiorcę.
    /// Zwraca false jeśli odbiorca nie jest przypisany do tej wiadomości.
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid messageId, Guid recipientMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Oznacza wszystkie nieprzeczytane wiadomości danego odbiorcy jako przeczytane.
    /// </summary>
    Task MarkAllAsReadAsync(Guid recipientMemberId, CancellationToken cancellationToken = default);
}
