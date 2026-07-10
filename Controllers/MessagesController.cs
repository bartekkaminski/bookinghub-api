using BookingHub.Api.Dtos.Message;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Wiadomości wewnętrzne między członkami organizacji.
///
/// Trasa bazowa: /api/organizations/{organizationId}/messages
///
///   GET /inbox          — skrzynka odbiorcza zalogowanego (wszyscy)
///   GET /outbox         — skrzynka nadawcza zalogowanego (wszyscy)
///   GET /unread-count   — liczba nieprzeczytanych (wszyscy)
///   GET /{messageId}    — szczegóły (nadawca lub odbiorca)
///
///   POST /              — wyślij wiadomość (wszyscy)
///   POST /{messageId}/reply — odpowiedz (nadawca lub odbiorca)
///   POST /{messageId}/read  — oznacz jako przeczytaną (odbiorca)
///   POST /read-all         — oznacz wszystkie jako przeczytane (wszyscy)
///   DELETE /{messageId}    — usuń (nadawca lub Admin)
/// </summary>
[Route("api/organizations/{organizationId:guid}/messages")]
[RequireOrgMembership]
public sealed class MessagesController : BookingHubControllerBase
{
    private readonly IMessageService _messages;

    public MessagesController(IMessageService messages)
    {
        _messages = messages;
    }

    /// <summary>
    /// Skrzynka odbiorcza zalogowanego członka w tej organizacji.
    /// </summary>
    [HttpGet("inbox")]
    [ProducesResponseType(typeof(PagedResult<MessageSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MessageSummaryResponse>>> GetInbox(
        Guid organizationId, [FromQuery] MessageFilterParams filter, CancellationToken ct)
    {
        var member = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember,
                "Nie jesteś członkiem tej organizacji.");

        var result = await _messages.GetInboxAsync(member.Id, filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Skrzynka nadawcza zalogowanego członka.
    /// </summary>
    [HttpGet("outbox")]
    [ProducesResponseType(typeof(PagedResult<MessageSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MessageSummaryResponse>>> GetOutbox(
        Guid organizationId, [FromQuery] MessageFilterParams filter, CancellationToken ct)
    {
        var member = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember,
                "Nie jesteś członkiem tej organizacji.");

        var result = await _messages.GetOutboxAsync(member.Id, filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Liczba nieprzeczytanych wiadomości zalogowanego członka.
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadCountResponse>> GetUnreadCount(
        Guid organizationId, CancellationToken ct)
    {
        var member = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember,
                "Nie jesteś członkiem tej organizacji.");

        var count = await _messages.GetUnreadCountAsync(member.Id, ct);
        return Ok(new UnreadCountResponse { UnreadCount = count });
    }

    /// <summary>
    /// Szczegóły wiadomości wraz z odbiorcami i odpowiedziami.
    /// Dostępne dla nadawcy lub odbiorcy wiadomości.
    /// </summary>
    [HttpGet("{messageId:guid}")]
    [ProducesResponseType(typeof(MessageDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDetailResponse>> GetById(
        Guid organizationId, Guid messageId, CancellationToken ct)
    {
        var member = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember,
                "Nie jesteś członkiem tej organizacji.");

        var message = await _messages.GetByIdAsync(messageId, member.Id, ct);
        return Ok(message);
    }

    /// <summary>
    /// Wysyła nową wiadomość do wybranych członków organizacji.
    /// Nadawcą staje się zalogowany użytkownik.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MessageDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDetailResponse>> Send(
        Guid organizationId, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var member = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember,
                "Nie jesteś członkiem tej organizacji.");

        var created = await _messages.SendAsync(organizationId, member.Id, request, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, messageId = created.Id }, created);
    }

    /// <summary>
    /// Odpowiada na wiadomość. Nadawcą odpowiedzi staje się zalogowany użytkownik.
    /// Dostępne dla każdego, kto widzi oryginalną wiadomość.
    /// </summary>
    [HttpPost("{messageId:guid}/reply")]
    [ProducesResponseType(typeof(MessageDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDetailResponse>> Reply(
        Guid organizationId, Guid messageId,
        [FromBody] ReplyMessageRequest request, CancellationToken ct)
    {
        var member = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember,
                "Nie jesteś członkiem tej organizacji.");

        var created = await _messages.ReplyAsync(messageId, member.Id, request, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, messageId = created.Id }, created);
    }

    /// <summary>
    /// Oznacza wiadomość jako przeczytaną przez zalogowanego odbiorcę.
    /// </summary>
    [HttpPost("{messageId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(
        Guid organizationId, Guid messageId, CancellationToken ct)
    {
        var member = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember,
                "Nie jesteś członkiem tej organizacji.");

        await _messages.MarkAsReadAsync(messageId, member.Id, ct);
        return NoContent();
    }

    /// <summary>
    /// Oznacza wszystkie nieprzeczytane wiadomości zalogowanego jako przeczytane.
    /// </summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(
        Guid organizationId, CancellationToken ct)
    {
        var member = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember,
                "Nie jesteś członkiem tej organizacji.");

        await _messages.MarkAllAsReadAsync(member.Id, ct);
        return NoContent();
    }

    /// <summary>
    /// Usuwa wiadomość (soft delete). Tylko nadawca może usunąć własną wiadomość.
    /// </summary>
    [HttpDelete("{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid organizationId, Guid messageId, CancellationToken ct)
    {
        var member = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember,
                "Nie jesteś członkiem tej organizacji.");

        await _messages.DeleteAsync(messageId, member.Id, ct);
        return NoContent();
    }
}
