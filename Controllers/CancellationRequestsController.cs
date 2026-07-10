using BookingHub.Api.Dtos.Cancellation;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Wnioski o odwołanie zapisu na zajęcia.
///
/// Trasa bazowa: /api/organizations/{organizationId}/cancellation-requests
///
/// Odczyt (Admin, Manager, Trainer):
///   GET /         — lista stronicowana
///   GET /pending  — tylko oczekujące (do obsługi przez trenera/admina)
///   GET /{requestId} — szczegóły
///
/// Dla uczestnika:
///   GET /my       — własne wnioski
///
/// Akcje:
///   POST /enrollments/{enrollmentId}  — złóż wniosek (uczestnik lub Admin)
///   POST /{requestId}/review          — rozpatrz wniosek (Admin, Manager, Trainer)
///   POST /{requestId}/withdraw        — cofnij wniosek (uczestnik lub Admin)
/// </summary>
[Route("api/organizations/{organizationId:guid}/cancellation-requests")]
[RequireOrgMembership]
public sealed class CancellationRequestsController : BookingHubControllerBase
{
    private readonly ICancellationRequestService _requests;

    public CancellationRequestsController(ICancellationRequestService requests)
    {
        _requests = requests;
    }

    /// <summary>
    /// Stronicowana lista wniosków dla organizacji.
    /// Admin, Manager lub Trainer widzi wszystkie.
    /// </summary>
    [HttpGet]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(PagedResult<CancellationRequestSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CancellationRequestSummaryResponse>>> GetPaged(
        Guid organizationId, [FromQuery] CancellationRequestFilterParams filter, CancellationToken ct)
    {
        var result = await _requests.GetPagedAsync(organizationId, filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lista oczekujących wniosków (Status = Pending) do obsługi przez trenera/admina.
    /// </summary>
    [HttpGet("pending")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(IReadOnlyList<CancellationRequestSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CancellationRequestSummaryResponse>>> GetPending(
        Guid organizationId, CancellationToken ct)
    {
        var result = await _requests.GetPendingForOrganizationAsync(organizationId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Własne wnioski zalogowanego uczestnika w tej organizacji.
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<CancellationRequestSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CancellationRequestSummaryResponse>>> GetMy(
        Guid organizationId, CancellationToken ct)
    {
        var member = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember,
                "Nie jesteś członkiem tej organizacji.");

        var result = await _requests.GetByMemberAsync(member.Id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Szczegóły wniosku. Admin/Manager/Trainer widzi każdy; uczestnik tylko swój.
    /// </summary>
    [HttpGet("{requestId:guid}")]
    [ProducesResponseType(typeof(CancellationRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CancellationRequestDetailResponse>> GetById(
        Guid organizationId, Guid requestId, CancellationToken ct)
    {
        var request = await _requests.GetByIdAsync(requestId, ct);
        if (request.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Wniosek {requestId} nie istnieje w tej organizacji.");

        var isPrivileged = await CurrentUser.IsAdminOrManagerAsync(organizationId, ct)
                        || await CurrentUser.IsTrainerAsync(organizationId, ct);

        if (!isPrivileged)
        {
            var myMember = await CurrentUser.GetMemberAsync(organizationId, ct);
            if (myMember?.Id != request.RequestedByMemberId)
                throw new ServiceException(ServiceErrorCode.Forbidden,
                    "Możesz przeglądać tylko własne wnioski.");
        }

        return Ok(request);
    }

    /// <summary>
    /// Składa wniosek o odwołanie zapisu.
    /// Uczestnik może złożyć tylko dla własnego zapisu. Admin/Manager może dla każdego.
    /// </summary>
    [HttpPost("enrollments/{enrollmentId:guid}")]
    [ProducesResponseType(typeof(CancellationRequestDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CancellationRequestDetailResponse>> Submit(
        Guid organizationId, Guid enrollmentId,
        [FromBody] CreateCancellationRequest body, CancellationToken ct)
    {
        var member = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember,
                "Nie jesteś członkiem tej organizacji.");

        var created = await _requests.RequestAsync(enrollmentId, member.Id, body, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, requestId = created.Id }, created);
    }

    /// <summary>
    /// Rozpatruje wniosek (Approved lub Rejected).
    /// Przy Approved: zapis uczestnika zmienia status na Cancelled.
    /// Admin, Manager lub Trainer.
    /// </summary>
    [HttpPost("{requestId:guid}/review")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(CancellationRequestDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CancellationRequestDetailResponse>> Review(
        Guid organizationId, Guid requestId,
        [FromBody] ReviewCancellationRequest body, CancellationToken ct)
    {
        var existing = await _requests.GetByIdAsync(requestId, ct);
        if (existing.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Wniosek {requestId} nie istnieje w tej organizacji.");

        var personId = RequirePersonId();
        var updated = await _requests.ReviewAsync(requestId, personId, body, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Cofa wniosek (Status → Withdrawn). Możliwe tylko gdy Status = Pending.
    /// Uczestnik może cofnąć tylko swój wniosek; Admin/Manager może każdy.
    /// </summary>
    [HttpPost("{requestId:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(
        Guid organizationId, Guid requestId, CancellationToken ct)
    {
        var request = await _requests.GetByIdAsync(requestId, ct);
        if (request.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Wniosek {requestId} nie istnieje w tej organizacji.");

        var isPrivileged = await CurrentUser.IsAdminOrManagerAsync(organizationId, ct);
        if (!isPrivileged)
        {
            var myMember = await CurrentUser.GetMemberAsync(organizationId, ct);
            if (myMember?.Id != request.RequestedByMemberId)
                throw new ServiceException(ServiceErrorCode.Forbidden,
                    "Możesz cofnąć tylko własny wniosek.");
        }

        await _requests.WithdrawAsync(requestId, ct);
        return NoContent();
    }
}
