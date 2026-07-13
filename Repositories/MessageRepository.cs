using BookingHub.Api.Data;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Repositories;

/// <summary>
/// Implementacja repozytorium wiadomości wewnętrznych (Message + MessageRecipient).
/// </summary>
public sealed class MessageRepository : BaseRepository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Message?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(m => m.Sender)
                .ThenInclude(s => s.Person)
            .Include(m => m.Recipients)
                .ThenInclude(r => r.Recipient)
                    .ThenInclude(rm => rm.Person)
            .Include(m => m.Replies.OrderBy(r => r.SentAt))
                .ThenInclude(r => r.Sender)
                    .ThenInclude(s => s.Person)
            .Include(m => m.Replies)
                .ThenInclude(r => r.Recipients)
            .Include(m => m.RelatedEvent)
            .Include(m => m.ParentMessage)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<Message?> GetWithRepliesAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(m => m.Sender)
                .ThenInclude(s => s.Person)
            .Include(m => m.Recipients)
                .ThenInclude(r => r.Recipient)
                    .ThenInclude(rm => rm.Person)
            .Include(m => m.Replies.OrderBy(r => r.SentAt))
                .ThenInclude(r => r.Sender)
                    .ThenInclude(s => s.Person)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<PagedResult<Message>> GetPagedAsync(MessageFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Include(m => m.Sender)
                .ThenInclude(s => s.Person)
            .AsQueryable();

        if (filter.OrganizationId.HasValue)
            query = query.Where(m => m.OrganizationId == filter.OrganizationId.Value);

        if (filter.SenderMemberId.HasValue)
            query = query.Where(m => m.SenderMemberId == filter.SenderMemberId.Value);

        if (filter.SentFrom.HasValue)
            query = query.Where(m => m.SentAt >= filter.SentFrom.Value);

        if (filter.SentTo.HasValue)
            query = query.Where(m => m.SentAt <= filter.SentTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLowerInvariant();
            query = query.Where(m => m.Subject.ToLower().Contains(search));
        }

        if (filter.IsAutomatic.HasValue)
            query = query.Where(m => m.IsAutomatic == filter.IsAutomatic.Value);

        if (filter.RelatedEventId.HasValue)
            query = query.Where(m => m.RelatedEventId == filter.RelatedEventId.Value);

        if (filter.OnlyRootMessages)
            query = query.Where(m => m.ParentMessageId == null);
        else if (filter.ParentMessageId.HasValue)
            query = query.Where(m => m.ParentMessageId == filter.ParentMessageId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.SentAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Message>(items, filter.Page, filter.PageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Message>> GetInboxAsync(Guid recipientMemberId, int page = 1, int pageSize = 20, bool onlyUnread = false, CancellationToken cancellationToken = default)
    {
        var recipientsQuery = _context.Set<MessageRecipient>()
            .AsNoTracking()
            .Where(r => r.RecipientMemberId == recipientMemberId);

        if (onlyUnread)
            recipientsQuery = recipientsQuery.Where(r => !r.IsRead);

        var messageIds = await recipientsQuery.Select(r => r.MessageId).ToListAsync(cancellationToken);

        var query = _dbSet
            .AsNoTracking()
            .Include(m => m.Sender)
                .ThenInclude(s => s.Person)
            .Where(m => messageIds.Contains(m.Id) && m.ParentMessageId == null);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Message>(items, page, pageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Message>> GetOutboxAsync(Guid senderMemberId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(m => m.Recipients)
                .ThenInclude(r => r.Recipient)
                    .ThenInclude(rm => rm.Person)
            .Where(m => m.SenderMemberId == senderMemberId && m.ParentMessageId == null);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Message>(items, page, pageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Message>> GetConversationsAsync(Guid memberId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var rootQuery = _dbSet
            .AsNoTracking()
            .Where(m => m.ParentMessageId == null &&
                        (m.SenderMemberId == memberId ||
                         m.Recipients.Any(r => r.RecipientMemberId == memberId)));

        var totalCount = await rootQuery.CountAsync(cancellationToken);

        // Wyznacz kolejność po dacie ostatniej aktywności (ostatnia odpowiedź lub oryginał)
        var orderedIds = await rootQuery
            .Select(m => new
            {
                m.Id,
                LastAt = m.Replies.Any()
                    ? (DateTime?)m.Replies.Max(r => r.SentAt)
                    : (DateTime?)m.SentAt,
            })
            .OrderByDescending(x => x.LastAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (orderedIds.Count == 0)
            return new PagedResult<Message>([], page, pageSize, totalCount);

        var items = await _dbSet
            .AsNoTracking()
            .Include(m => m.Sender).ThenInclude(s => s.Person)
            .Include(m => m.Recipients).ThenInclude(r => r.Recipient).ThenInclude(rm => rm.Person)
            .Include(m => m.Replies.OrderByDescending(r => r.SentAt))
                .ThenInclude(r => r.Sender).ThenInclude(s => s.Person)
            .Include(m => m.Replies)
                .ThenInclude(r => r.Recipients)
            .Include(m => m.RelatedEvent)
            .Where(m => orderedIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        // Zachowaj kolejność wyznaczoną przez orderedIds
        var ordered = orderedIds.Select(id => items.First(m => m.Id == id)).ToList();
        return new PagedResult<Message>(ordered, page, pageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Message>> GetByRelatedEventAsync(Guid eventId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
            .Include(m => m.Sender)
                .ThenInclude(s => s.Person)
            .Where(m => m.RelatedEventId == eventId)
            .OrderBy(m => m.SentAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<int> GetUnreadCountAsync(Guid recipientMemberId, CancellationToken cancellationToken = default)
        => await _context.Set<MessageRecipient>()
            .Where(r => r.RecipientMemberId == recipientMemberId && !r.IsRead)
            .CountAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> MarkAsReadAsync(Guid messageId, Guid recipientMemberId, CancellationToken cancellationToken = default)
    {
        var recipient = await _context.Set<MessageRecipient>()
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.RecipientMemberId == recipientMemberId, cancellationToken);

        if (recipient is null) return false;

        recipient.IsRead = true;
        recipient.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task MarkAllAsReadAsync(Guid recipientMemberId, CancellationToken cancellationToken = default)
    {
        var unread = await _context.Set<MessageRecipient>()
            .Where(r => r.RecipientMemberId == recipientMemberId && !r.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var r in unread)
        {
            r.IsRead = true;
            r.ReadAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
