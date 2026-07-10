using BookingHub.Api.Dtos.Enrollment;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie zapisami na zajęcia (indywidualnymi i zespołowymi).
///
/// Trasa bazowa: /api/organizations/{organizationId}/events/{eventId}/enrollments
///
/// Odczyt:
///   GET /              — zapisy indywidualne dla zajęć (Admin, Manager, Trainer)
///   GET /teams         — zapisy zespołów dla zajęć (Admin, Manager, Trainer)
///   GET /{enrollmentId} — szczegóły zapisu (Admin, Manager, Trainer lub właściciel)
///
/// Zarządzanie:
///   POST /enroll-member       — zapisz uczestnika (Admin, Manager, Trainer lub uczestnik samodzielnie)
///   POST /enroll-team         — zapisz zespół (Admin, Manager, Trainer)
///   DELETE /{enrollmentId}    — wypisz uczestnika (Admin, Manager, Trainer lub właściciel)
///   DELETE /teams/{teamEnrollmentId} — wypisz zespół (Admin, Manager, Trainer)
///   PATCH /{enrollmentId}/status — zmień status zapisu (Admin, Manager, Trainer)
///   POST  /bulk-attendance    — zbiorowe oznaczanie obecności (Admin, Manager, Trainer)
///
/// Historia uczestnika:
///   GET /members/{memberId} — historia zapisów uczestnika (Admin, Manager, Trainer lub właściciel)
/// </summary>
[Route("api/organizations/{organizationId:guid}/events/{eventId:guid}/enrollments")]
[RequireOrgMembership]
public sealed class EnrollmentsController : BookingHubControllerBase
{
    private readonly IEnrollmentService _enrollments;

    public EnrollmentsController(IEnrollmentService enrollments)
    {
        _enrollments = enrollments;
    }

    /// <summary>
    /// Stronicowana lista zapisów indywidualnych dla zajęć.
    /// </summary>
    [HttpGet]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(PagedResult<EnrollmentSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EnrollmentSummaryResponse>>> GetForEvent(
        Guid organizationId, Guid eventId,
        [FromQuery] EventEnrollmentFilterParams filter, CancellationToken ct)
    {
        var result = await _enrollments.GetPagedForEventAsync(eventId, filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lista zapisów zespołów dla zajęć.
    /// </summary>
    [HttpGet("teams")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(IReadOnlyList<TeamEnrollmentSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeamEnrollmentSummaryResponse>>> GetTeamEnrollments(
        Guid organizationId, Guid eventId, CancellationToken ct)
    {
        var result = await _enrollments.GetTeamEnrollmentsForEventAsync(eventId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Szczegóły zapisu indywidualnego. Admin/Manager/Trainer widzi każdy; uczestnik tylko swój.
    /// </summary>
    [HttpGet("{enrollmentId:guid}")]
    [ProducesResponseType(typeof(EnrollmentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentDetailResponse>> GetById(
        Guid organizationId, Guid eventId, Guid enrollmentId, CancellationToken ct)
    {
        var enrollment = await _enrollments.GetByIdAsync(enrollmentId, ct);

        // Weryfikacja że zapis należy do tej organizacji i tych zajęć.
        if (enrollment.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Zapis {enrollmentId} nie istnieje w tej organizacji.");
        if (enrollment.EventId != eventId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Zapis {enrollmentId} nie należy do tych zajęć.");

        var isAdminOrManager = await CurrentUser.IsAdminOrManagerAsync(organizationId, ct);
        if (!isAdminOrManager && !await CurrentUser.IsTrainerAsync(organizationId, ct))
        {
            var myMember = await CurrentUser.GetMemberAsync(organizationId, ct);
            if (myMember?.Id != enrollment.OrganizationMemberId)
                throw new ServiceException(ServiceErrorCode.Forbidden,
                    "Możesz przeglądać tylko własne zapisy.");
        }

        return Ok(enrollment);
    }

    /// <summary>
    /// Zapisuje uczestnika na zajęcia.
    /// Admin/Manager/Trainer może zapisać dowolnego uczestnika.
    /// Participant może zapisać tylko siebie (OrganizationMemberId musi być jego własnym).
    /// </summary>
    [HttpPost("enroll-member")]
    [ProducesResponseType(typeof(EnrollmentDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrollmentDetailResponse>> EnrollMember(
        Guid organizationId, Guid eventId,
        [FromBody] EnrollMemberRequest request, CancellationToken ct)
    {
        var isPrivileged = await CurrentUser.IsAdminOrManagerAsync(organizationId, ct)
                        || await CurrentUser.IsTrainerAsync(organizationId, ct);

        if (!isPrivileged)
        {
            var myMember = await CurrentUser.GetMemberAsync(organizationId, ct);
            if (myMember?.Id != request.OrganizationMemberId)
                throw new ServiceException(ServiceErrorCode.Forbidden,
                    "Możesz zapisać tylko siebie samego.");
        }

        var created = await _enrollments.EnrollMemberAsync(eventId, request.OrganizationMemberId, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, eventId, enrollmentId = created.Id }, created);
    }

    /// <summary>
    /// Zapisuje cały zespół na zajęcia. Admin, Manager lub Trainer.
    /// Tworzy EventTeamEnrollment + indywidualne zapisy dla każdego członka.
    /// </summary>
    [HttpPost("enroll-team")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(TeamEnrollmentSummaryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeamEnrollmentSummaryResponse>> EnrollTeam(
        Guid organizationId, Guid eventId,
        [FromBody] EnrollTeamRequest request, CancellationToken ct)
    {
        var created = await _enrollments.EnrollTeamAsync(eventId, request.TeamId, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>
    /// Wypisuje uczestnika z zajęć (status → Cancelled).
    /// Admin/Manager/Trainer może wypisać każdego; uczestnik tylko siebie.
    /// </summary>
    [HttpDelete("{enrollmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unenroll(
        Guid organizationId, Guid eventId, Guid enrollmentId, CancellationToken ct)
    {
        var enrollment = await _enrollments.GetByIdAsync(enrollmentId, ct);

        var isPrivileged = await CurrentUser.IsAdminOrManagerAsync(organizationId, ct)
                        || await CurrentUser.IsTrainerAsync(organizationId, ct);

        if (!isPrivileged)
        {
            var myMember = await CurrentUser.GetMemberAsync(organizationId, ct);
            if (myMember?.Id != enrollment.OrganizationMemberId)
                throw new ServiceException(ServiceErrorCode.Forbidden,
                    "Możesz wypisać tylko siebie samego.");
        }

        await _enrollments.UnenrollMemberAsync(enrollmentId, ct);
        return NoContent();
    }

    /// <summary>
    /// Wypisuje cały zespół z zajęć. Admin, Manager lub Trainer.
    /// </summary>
    [HttpDelete("teams/{teamEnrollmentId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnenrollTeam(
        Guid organizationId, Guid eventId, Guid teamEnrollmentId, CancellationToken ct)
    {
        await _enrollments.UnenrollTeamAsync(teamEnrollmentId, ct);
        return NoContent();
    }

    /// <summary>
    /// Zmienia status zapisu (np. Attended, Absent). Admin, Manager lub Trainer.
    /// </summary>
    [HttpPatch("{enrollmentId:guid}/status")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(EnrollmentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentDetailResponse>> SetStatus(
        Guid organizationId, Guid eventId, Guid enrollmentId,
        [FromBody] SetEnrollmentStatusRequest request, CancellationToken ct)
    {
        var updated = await _enrollments.SetStatusAsync(enrollmentId, request.Status, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Zbiorcze oznaczanie obecności — zaznacza podane zapisy jako Attended.
    /// Admin, Manager lub Trainer.
    /// </summary>
    [HttpPost("bulk-attendance")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BulkMarkAttended(
        Guid organizationId, Guid eventId,
        [FromBody] BulkAttendanceRequest request, CancellationToken ct)
    {
        await _enrollments.BulkMarkAttendedAsync(eventId, request, ct);
        return NoContent();
    }

    /// <summary>
    /// Uczestnik składa wniosek o zapis (status PendingApproval — wymaga zatwierdzenia trenera).
    /// Dostępne dla każdego członka organizacji; uczestnik może złożyć wniosek tylko dla siebie.
    /// </summary>
    [HttpPost("request-enrollment")]
    [ProducesResponseType(typeof(EnrollmentDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrollmentDetailResponse>> RequestEnrollment(
        Guid organizationId, Guid eventId,
        [FromBody] RequestEnrollmentRequest request, CancellationToken ct)
    {
        var myMember = await CurrentUser.GetMemberAsync(organizationId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotMember, "Nie jesteś członkiem tej organizacji.");

        var created = await _enrollments.RequestEnrollmentAsync(
            eventId, myMember.Id, request.Reason, ct);

        return CreatedAtAction(nameof(GetById),
            new { organizationId, eventId, enrollmentId = created.Id }, created);
    }

    /// <summary>
    /// Zatwierdza wniosek o zapis (PendingApproval → Enrolled). Admin, Manager lub Trainer.
    /// </summary>
    [HttpPost("{enrollmentId:guid}/approve")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(EnrollmentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentDetailResponse>> ApproveRequest(
        Guid organizationId, Guid eventId, Guid enrollmentId,
        [FromBody] ReviewEnrollmentRequestRequest request, CancellationToken ct)
    {
        var updated = await _enrollments.ApproveEnrollmentRequestAsync(enrollmentId, request.ReviewNote, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Odrzuca wniosek o zapis (PendingApproval → Cancelled). Admin, Manager lub Trainer.
    /// </summary>
    [HttpPost("{enrollmentId:guid}/reject")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(EnrollmentDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentDetailResponse>> RejectRequest(
        Guid organizationId, Guid eventId, Guid enrollmentId,
        [FromBody] ReviewEnrollmentRequestRequest request, CancellationToken ct)
    {
        var updated = await _enrollments.RejectEnrollmentRequestAsync(enrollmentId, request.ReviewNote, ct);
        return Ok(updated);
    }
}

/// <summary>
/// Oczekujące wnioski o zapis na poziomie organizacji.
/// Trasa: /api/organizations/{organizationId}/enrollment-requests
/// </summary>
[Route("api/organizations/{organizationId:guid}/enrollment-requests")]
[RequireOrgMembership]
public sealed class EnrollmentRequestsController : BookingHubControllerBase
{
    private readonly IEnrollmentService _enrollments;

    public EnrollmentRequestsController(IEnrollmentService enrollments)
    {
        _enrollments = enrollments;
    }

    /// <summary>
    /// Lista oczekujących wniosków o zapis w organizacji. Admin, Manager lub Trainer.
    /// </summary>
    [HttpGet("pending")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentRequestSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EnrollmentRequestSummaryResponse>>> GetPending(
        Guid organizationId, CancellationToken ct)
    {
        var result = await _enrollments.GetPendingRequestsForOrganizationAsync(organizationId, ct);
        return Ok(result);
    }
}

/// <summary>
/// Dodatkowy kontroler do odczytu historii zapisów danego uczestnika.
/// Trasa: /api/organizations/{organizationId}/members/{memberId}/enrollments
/// </summary>
[Route("api/organizations/{organizationId:guid}/members/{memberId:guid}/enrollments")]
[RequireOrgMembership]
public sealed class MemberEnrollmentsController : BookingHubControllerBase
{
    private readonly IEnrollmentService _enrollments;

    public MemberEnrollmentsController(IEnrollmentService enrollments)
    {
        _enrollments = enrollments;
    }

    /// <summary>
    /// Historia zapisów uczestnika (stronicowana).
    /// Admin/Manager/Trainer widzi każdego; uczestnik tylko siebie.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EnrollmentSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<EnrollmentSummaryResponse>>> GetForMember(
        Guid organizationId, Guid memberId,
        [FromQuery] EventEnrollmentFilterParams filter, CancellationToken ct)
    {
        var isPrivileged = await CurrentUser.IsAdminOrManagerAsync(organizationId, ct)
                        || await CurrentUser.IsTrainerAsync(organizationId, ct);

        if (!isPrivileged)
        {
            var myMember = await CurrentUser.GetMemberAsync(organizationId, ct);
            if (myMember?.Id != memberId)
                throw new ServiceException(ServiceErrorCode.Forbidden,
                    "Możesz przeglądać tylko swoją historię zapisów.");
        }

        var result = await _enrollments.GetPagedForMemberAsync(memberId, filter, ct);
        return Ok(result);
    }
}
