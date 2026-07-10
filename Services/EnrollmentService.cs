using BookingHub.Api.Dtos.Enrollment;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania zapisami na zajęcia (indywidualnymi i zespołowymi).
/// </summary>
public sealed class EnrollmentService : IEnrollmentService
{
    private readonly IEventEnrollmentRepository _enrollments;
    private readonly IEventTeamEnrollmentRepository _teamEnrollments;
    private readonly IEventRepository _events;
    private readonly IOrganizationMemberRepository _members;
    private readonly ITeamRepository _teams;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(
        IEventEnrollmentRepository enrollments,
        IEventTeamEnrollmentRepository teamEnrollments,
        IEventRepository events,
        IOrganizationMemberRepository members,
        ITeamRepository teams,
        ILogger<EnrollmentService> logger)
    {
        _enrollments     = enrollments;
        _teamEnrollments = teamEnrollments;
        _events          = events;
        _members         = members;
        _teams           = teams;
        _logger          = logger;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<EnrollmentSummaryResponse>> GetPagedForEventAsync(Guid eventId, EventEnrollmentFilterParams filter, CancellationToken ct = default)
    {
        filter.EventId = eventId;
        var paged = await _enrollments.GetPagedAsync(filter, ct);
        return paged.Map(e => e.ToSummary());
    }

    /// <inheritdoc/>
    public async Task<PagedResult<EnrollmentSummaryResponse>> GetPagedForMemberAsync(Guid memberId, EventEnrollmentFilterParams filter, CancellationToken ct = default)
    {
        var paged = await _enrollments.GetByMemberPagedAsync(memberId, filter, ct);
        return paged.Map(e => e.ToSummary());
    }

    /// <inheritdoc/>
    public async Task<EnrollmentDetailResponse> GetByIdAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await _enrollments.GetWithDetailsAsync(enrollmentId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zapis {enrollmentId} nie istnieje.");
        return enrollment.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<EnrollmentDetailResponse> EnrollMemberAsync(Guid eventId, Guid organizationMemberId, CancellationToken ct = default)
    {
        var ev = await _events.GetByIdAsync(eventId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zajęcia {eventId} nie istnieją.");

        if (ev.Status == EventStatus.Cancelled)
            throw new ServiceException(ServiceErrorCode.EventCancelled,
                "Nie można zapisać na odwołane zajęcia.");

        if (ev.Status == EventStatus.Completed)
            throw new ServiceException(ServiceErrorCode.EventCompleted,
                "Nie można zapisać na zakończone zajęcia.");

        var member = await _members.GetByIdAsync(organizationMemberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Uczestnik {organizationMemberId} nie istnieje.");

        // Uczestnik musi należeć do tej samej organizacji co zajęcia.
        if (member.OrganizationId != ev.OrganizationId)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Uczestnik nie należy do organizacji tych zajęć.");

        if (!member.IsActive)
            throw new ServiceException(ServiceErrorCode.AccountInactive,
                "Konto uczestnika jest nieaktywne w tej organizacji.");

        var alreadyEnrolled = await _enrollments.IsEnrolledAsync(eventId, organizationMemberId, ct);
        if (alreadyEnrolled)
            throw new ServiceException(ServiceErrorCode.MemberAlreadyEnrolled,
                "Uczestnik jest już zapisany na te zajęcia.");

        var enrollment = new EventEnrollment
        {
            EventId              = eventId,
            OrganizationMemberId = organizationMemberId,
            Status               = EventEnrollmentStatus.Enrolled,
        };
        var created = await _enrollments.AddAsync(enrollment, ct);
        var details = await _enrollments.GetWithDetailsAsync(created.Id, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<TeamEnrollmentSummaryResponse> EnrollTeamAsync(Guid eventId, Guid teamId, CancellationToken ct = default)
    {
        var ev = await _events.GetByIdAsync(eventId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zajęcia {eventId} nie istnieją.");

        if (ev.Status == EventStatus.Cancelled)
            throw new ServiceException(ServiceErrorCode.EventCancelled, "Nie można zapisać na odwołane zajęcia.");

        if (ev.Status == EventStatus.Completed)
            throw new ServiceException(ServiceErrorCode.EventCompleted, "Nie można zapisać na zakończone zajęcia.");

        var team = await _teams.GetWithDetailsAsync(teamId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zespół {teamId} nie istnieje.");

        // Zespół musi należeć do tej samej organizacji co zajęcia.
        if (team.OrganizationId != ev.OrganizationId)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Zespół nie należy do organizacji tych zajęć.");

        if (!team.IsActive)
            throw new ServiceException(ServiceErrorCode.Conflict, "Zespół jest nieaktywny.");

        if (await _teamEnrollments.IsEnrolledAsync(eventId, teamId, ct))
            throw new ServiceException(ServiceErrorCode.TeamAlreadyEnrolled,
                "Zespół jest już zapisany na te zajęcia.");

        // Utwórz TeamEnrollment
        var teamEnrollment = new EventTeamEnrollment
        {
            EventId = eventId,
            TeamId  = teamId,
            Status  = EventEnrollmentStatus.Enrolled,
        };
        teamEnrollment = await _teamEnrollments.AddAsync(teamEnrollment, ct);

        // Utwórz indywidualne zapisy dla każdego aktywnego członka zespołu
        foreach (var teamMember in team.Members)
        {
            var alreadyEnrolled = await _enrollments.IsEnrolledAsync(eventId, teamMember.OrganizationMemberId, ct);
            if (!alreadyEnrolled)
            {
                var enrollment = new EventEnrollment
                {
                    EventId              = eventId,
                    OrganizationMemberId = teamMember.OrganizationMemberId,
                    Status               = EventEnrollmentStatus.Enrolled,
                };
                await _enrollments.AddAsync(enrollment, ct);
            }
        }

        var details = await _teamEnrollments.GetWithDetailsAsync(teamEnrollment.Id, ct);
        return details!.ToSummary();
    }

    /// <inheritdoc/>
    public async Task UnenrollMemberAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await _enrollments.GetByIdAsync(enrollmentId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zapis {enrollmentId} nie istnieje.");

        if (enrollment.Status == EventEnrollmentStatus.Cancelled)
            throw new ServiceException(ServiceErrorCode.EnrollmentNotActive, "Zapis jest już anulowany.");

        enrollment.Status = EventEnrollmentStatus.Cancelled;
        await _enrollments.UpdateAsync(enrollment, ct);
    }

    /// <inheritdoc/>
    public async Task UnenrollTeamAsync(Guid teamEnrollmentId, CancellationToken ct = default)
    {
        var teamEnrollment = await _teamEnrollments.GetWithDetailsAsync(teamEnrollmentId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zapis zespołu {teamEnrollmentId} nie istnieje.");

        if (teamEnrollment.Status == EventEnrollmentStatus.Cancelled)
            throw new ServiceException(ServiceErrorCode.EnrollmentNotActive, "Zapis jest już anulowany.");

        teamEnrollment.Status = EventEnrollmentStatus.Cancelled;
        await _teamEnrollments.UpdateAsync(teamEnrollment, ct);

        // Anuluj indywidualne zapisy członków tego zespołu (jeśli nie zapisani z innego źródła)
        if (teamEnrollment.Team is not null)
        {
            foreach (var teamMember in teamEnrollment.Team.Members)
            {
                var memberEnrollment = await _enrollments.GetActiveByEventAndMemberAsync(
                    teamEnrollment.EventId, teamMember.OrganizationMemberId, ct);
                if (memberEnrollment is not null)
                {
                    memberEnrollment.Status = EventEnrollmentStatus.Cancelled;
                    await _enrollments.UpdateAsync(memberEnrollment, ct);
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TeamEnrollmentSummaryResponse>> GetTeamEnrollmentsForEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var teamEnrollments = await _teamEnrollments.GetByEventAsync(eventId, null, ct);
        return teamEnrollments.Select(te => te.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<EnrollmentDetailResponse> SetStatusAsync(Guid enrollmentId, EventEnrollmentStatus status, CancellationToken ct = default)
    {
        var enrollment = await _enrollments.GetByIdAsync(enrollmentId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zapis {enrollmentId} nie istnieje.");

        enrollment.Status = status;
        await _enrollments.UpdateAsync(enrollment, ct);

        var details = await _enrollments.GetWithDetailsAsync(enrollmentId, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task BulkMarkAttendedAsync(Guid eventId, BulkAttendanceRequest request, CancellationToken ct = default)
    {
        var ev = await _events.GetByIdAsync(eventId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zajęcia {eventId} nie istnieją.");

        foreach (var enrollmentId in request.EnrollmentIds)
        {
            var enrollment = await _enrollments.GetByIdAsync(enrollmentId, ct);
            if (enrollment is null || enrollment.EventId != eventId)
            {
                _logger.LogWarning("BulkMarkAttended: Zapis {EnrollmentId} nie należy do zajęć {EventId}.",
                    enrollmentId, eventId);
                continue;
            }

            if (enrollment.Status == EventEnrollmentStatus.Enrolled)
            {
                enrollment.Status = EventEnrollmentStatus.Attended;
                await _enrollments.UpdateAsync(enrollment, ct);
            }
        }
    }
}
