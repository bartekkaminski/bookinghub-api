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
    private readonly IMessageService _messages;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(
        IEventEnrollmentRepository enrollments,
        IEventTeamEnrollmentRepository teamEnrollments,
        IEventRepository events,
        IOrganizationMemberRepository members,
        ITeamRepository teams,
        IMessageService messages,
        ILogger<EnrollmentService> logger)
    {
        _enrollments     = enrollments;
        _teamEnrollments = teamEnrollments;
        _events          = events;
        _members         = members;
        _teams           = teams;
        _messages        = messages;
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

        await NotifyEnrolledAsync(ev, [organizationMemberId], ct);

        var details = await _enrollments.GetWithDetailsAsync(created.Id, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task EnrollMembersOnCreateAsync(Guid eventId, IReadOnlyList<Guid> memberIds, CancellationToken ct = default)
    {
        if (memberIds.Count == 0) return;

        var ev = await _events.GetByIdAsync(eventId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zajęcia {eventId} nie istnieją.");

        var newlyEnrolled = new List<Guid>();
        foreach (var memberId in memberIds.Distinct())
        {
            if (await _enrollments.IsEnrolledAsync(eventId, memberId, ct))
                continue;

            var member = await _members.GetByIdAsync(memberId, ct);
            if (member is null || member.OrganizationId != ev.OrganizationId || !member.IsActive)
                continue;

            await _enrollments.AddAsync(new EventEnrollment
            {
                EventId              = eventId,
                OrganizationMemberId = memberId,
                Status               = EventEnrollmentStatus.Enrolled,
            }, ct);
            newlyEnrolled.Add(memberId);
        }

        await NotifyEnrolledAsync(ev, newlyEnrolled, ct);
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
        var newlyEnrolled = new List<Guid>();
        foreach (var teamMember in team.Members)
        {
            var memberId = teamMember.OrganizationMemberId;
            if (teamMember.OrganizationMember is { IsActive: false })
                continue;

            var alreadyEnrolled = await _enrollments.IsEnrolledAsync(eventId, memberId, ct);
            if (!alreadyEnrolled)
            {
                var enrollment = new EventEnrollment
                {
                    EventId              = eventId,
                    OrganizationMemberId = memberId,
                    Status               = EventEnrollmentStatus.Enrolled,
                };
                await _enrollments.AddAsync(enrollment, ct);
                newlyEnrolled.Add(memberId);
            }
        }

        await NotifyEnrolledAsync(ev, newlyEnrolled, ct);

        var details = await _teamEnrollments.GetWithDetailsAsync(teamEnrollment.Id, ct);
        return details!.ToSummary();
    }

    /// <inheritdoc/>
    public async Task EnrollTeamsOnCreateAsync(Guid eventId, IReadOnlyList<Guid> teamIds, CancellationToken ct = default)
    {
        if (teamIds.Count == 0) return;

        foreach (var teamId in teamIds.Distinct())
        {
            if (await _teamEnrollments.IsEnrolledAsync(eventId, teamId, ct))
                continue;

            try
            {
                await EnrollTeamAsync(eventId, teamId, ct);
            }
            catch (ServiceException ex) when (
                ex.ErrorCode is ServiceErrorCode.TeamAlreadyEnrolled
                    or ServiceErrorCode.NotFound
                    or ServiceErrorCode.Conflict
                    or ServiceErrorCode.ValidationError)
            {
                _logger.LogDebug(ex, "Pominięto zapis zespołu {TeamId} na zajęcia {EventId}.", teamId, eventId);
            }
        }
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

        var ev = await _events.GetByIdAsync(enrollment.EventId, ct);
        if (ev is not null)
            await NotifyUnenrolledAsync(ev, [enrollment.OrganizationMemberId], ct);
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

        var unenrolledMemberIds = new List<Guid>();

        // Anuluj indywidualne zapisy członków tego zespołu
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
                    unenrolledMemberIds.Add(teamMember.OrganizationMemberId);
                }
            }
        }

        var ev = teamEnrollment.Event
            ?? await _events.GetByIdAsync(teamEnrollment.EventId, ct);
        if (ev is not null)
            await NotifyUnenrolledAsync(ev, unenrolledMemberIds, ct);
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

        var previous = enrollment.Status;
        enrollment.Status = status;
        await _enrollments.UpdateAsync(enrollment, ct);

        // Powiadomienie tylko przy zapisany / wypisany — nie przy Obecny / Nieobecny
        if (previous != status &&
            status is EventEnrollmentStatus.Enrolled or EventEnrollmentStatus.Cancelled)
        {
            var ev = await _events.GetByIdAsync(enrollment.EventId, ct);
            if (ev is not null)
            {
                if (status == EventEnrollmentStatus.Enrolled)
                    await NotifyEnrolledAsync(ev, [enrollment.OrganizationMemberId], ct);
                else
                    await NotifyUnenrolledAsync(ev, [enrollment.OrganizationMemberId], ct);
            }
        }

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

    // ── Wnioski o zapis (PendingApproval) ─────────────────────────────────────

    /// <inheritdoc/>
    public async Task<EnrollmentDetailResponse> RequestEnrollmentAsync(Guid eventId, Guid organizationMemberId, string? reason, CancellationToken ct = default)
    {
        var ev = await _events.GetByIdAsync(eventId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zajęcia {eventId} nie istnieją.");

        if (ev.Status == EventStatus.Cancelled)
            throw new ServiceException(ServiceErrorCode.EventCancelled, "Nie można złożyć wniosku na odwołane zajęcia.");

        if (ev.Status == EventStatus.Completed)
            throw new ServiceException(ServiceErrorCode.EventCompleted, "Nie można złożyć wniosku na zakończone zajęcia.");

        var member = await _members.GetByIdAsync(organizationMemberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Uczestnik {organizationMemberId} nie istnieje.");

        if (member.OrganizationId != ev.OrganizationId)
            throw new ServiceException(ServiceErrorCode.ValidationError, "Uczestnik nie należy do organizacji tych zajęć.");

        if (!member.IsActive)
            throw new ServiceException(ServiceErrorCode.AccountInactive, "Konto uczestnika jest nieaktywne.");

        // Czy uczestnik jest już zapisany lub ma oczekujący wniosek?
        if (await _enrollments.IsEnrolledAsync(eventId, organizationMemberId, ct))
            throw new ServiceException(ServiceErrorCode.MemberAlreadyEnrolled, "Uczestnik jest już zapisany na te zajęcia.");

        if (await _enrollments.HasPendingRequestAsync(eventId, organizationMemberId, ct))
            throw new ServiceException(ServiceErrorCode.Conflict, "Oczekujący wniosek o zapis już istnieje.");

        var enrollment = new EventEnrollment
        {
            EventId              = eventId,
            OrganizationMemberId = organizationMemberId,
            Status               = EventEnrollmentStatus.PendingApproval,
        };

        var created = await _enrollments.AddAsync(enrollment, ct);
        var details = await _enrollments.GetWithDetailsAsync(created.Id, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<EnrollmentDetailResponse> ApproveEnrollmentRequestAsync(Guid enrollmentId, string? reviewNote, CancellationToken ct = default)
    {
        var enrollment = await _enrollments.GetByIdAsync(enrollmentId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zapis {enrollmentId} nie istnieje.");

        if (enrollment.Status != EventEnrollmentStatus.PendingApproval)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Można zatwierdzać tylko wnioski ze statusem PendingApproval.");

        enrollment.Status = EventEnrollmentStatus.Enrolled;
        await _enrollments.UpdateAsync(enrollment, ct);

        _logger.LogInformation("Wniosek o zapis {EnrollmentId} zatwierdzony. Notatka: {Note}", enrollmentId, reviewNote);

        var ev = await _events.GetByIdAsync(enrollment.EventId, ct);
        if (ev is not null)
            await NotifyEnrolledAsync(ev, [enrollment.OrganizationMemberId], ct);

        var details = await _enrollments.GetWithDetailsAsync(enrollmentId, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<EnrollmentDetailResponse> RejectEnrollmentRequestAsync(Guid enrollmentId, string? reviewNote, CancellationToken ct = default)
    {
        var enrollment = await _enrollments.GetByIdAsync(enrollmentId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zapis {enrollmentId} nie istnieje.");

        if (enrollment.Status != EventEnrollmentStatus.PendingApproval)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Można odrzucać tylko wnioski ze statusem PendingApproval.");

        enrollment.Status = EventEnrollmentStatus.Cancelled;
        await _enrollments.UpdateAsync(enrollment, ct);

        _logger.LogInformation("Wniosek o zapis {EnrollmentId} odrzucony. Notatka: {Note}", enrollmentId, reviewNote);

        var ev = await _events.GetByIdAsync(enrollment.EventId, ct);
        if (ev is not null)
            await NotifyEnrollmentRequestRejectedAsync(ev, enrollment.OrganizationMemberId, reviewNote, ct);

        var details = await _enrollments.GetWithDetailsAsync(enrollmentId, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EnrollmentRequestSummaryResponse>> GetPendingRequestsForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var pending = await _enrollments.GetPendingRequestsForOrganizationAsync(organizationId, ct);
        return pending.Select(e => e.ToRequestSummary()).ToList();
    }

    // ── Powiadomienia ────────────────────────────────────────────────────────

    private async Task NotifyEnrolledAsync(Event ev, IReadOnlyList<Guid> memberIds, CancellationToken ct)
    {
        if (memberIds.Count == 0) return;

        try
        {
            await _messages.SendSystemMessageAsync(
                ev.OrganizationId,
                $"Zapis na zajęcia: {ev.Title}",
                $"Zostałeś/aś zapisany/a na zajęcia \"{ev.Title}\" zaplanowane na {ev.StartTime:dd.MM.yyyy HH:mm}.",
                memberIds,
                ev.Id,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nie udało się wysłać powiadomienia o zapisie na zajęcia {EventId}.", ev.Id);
        }
    }

    private async Task NotifyUnenrolledAsync(Event ev, IReadOnlyList<Guid> memberIds, CancellationToken ct)
    {
        if (memberIds.Count == 0) return;

        try
        {
            await _messages.SendSystemMessageAsync(
                ev.OrganizationId,
                $"Wypisanie z zajęć: {ev.Title}",
                $"Zostałeś/aś wypisany/a z zajęć \"{ev.Title}\" zaplanowanych na {ev.StartTime:dd.MM.yyyy HH:mm}.",
                memberIds,
                ev.Id,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nie udało się wysłać powiadomienia o wypisaniu z zajęć {EventId}.", ev.Id);
        }
    }

    private async Task NotifyEnrollmentRequestRejectedAsync(Event ev, Guid memberId, string? reviewNote, CancellationToken ct)
    {
        var note = string.IsNullOrWhiteSpace(reviewNote) ? "" : $"\n\nNotatka: {reviewNote.Trim()}";
        try
        {
            await _messages.SendSystemMessageAsync(
                ev.OrganizationId,
                $"Wniosek o zapis odrzucony: {ev.Title}",
                $"Twój wniosek o zapis na zajęcia \"{ev.Title}\" ({ev.StartTime:dd.MM.yyyy HH:mm}) został odrzucony.{note}",
                [memberId],
                ev.Id,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nie udało się wysłać powiadomienia o odrzuceniu wniosku o zapis {EventId}.", ev.Id);
        }
    }
}
