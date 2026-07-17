using BookingHub.Api.Dtos.Event;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania zajęciami (jednorazowymi i cyklicznymi).
/// </summary>
public sealed class EventService : IEventService
{
    private const int MaxRecurringEvents = 500;
    private const int MaxRecurringRangeDays = 365;
    private const int MaxCreateMembers = 200;
    private const int MaxCreateTeams = 50;

    private readonly IEventRepository _events;
    private readonly IOrganizationRepository _organizations;
    private readonly IOrganizationMemberRepository _members;
    private readonly IEventEnrollmentRepository _enrollments;
    private readonly ILocationRepository _locations;
    private readonly IGroupRepository _groups;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IMessageService _messages;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IEventRepository events,
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IEventEnrollmentRepository enrollments,
        ILocationRepository locations,
        IGroupRepository groups,
        IEnrollmentService enrollmentService,
        IMessageService messages,
        ILogger<EventService> logger)
    {
        _events            = events;
        _organizations     = organizations;
        _members           = members;
        _enrollments       = enrollments;
        _locations         = locations;
        _groups            = groups;
        _enrollmentService = enrollmentService;
        _messages          = messages;
        _logger            = logger;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<EventSummaryResponse>> GetPagedAsync(Guid organizationId, EventFilterParams filter, CancellationToken ct = default)
    {
        filter.OrganizationId = organizationId;
        var paged = await _events.GetPagedAsync(filter, ct);
        return paged.Map(e => e.ToSummary());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EventCalendarResponse>> GetCalendarAsync(Guid organizationId, CalendarRequest request, CancellationToken ct = default)
    {
        var events = await _events.GetCalendarAsync(organizationId, request.From, request.To, ct);
        return events.Select(e => e.ToCalendar()).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EventCalendarResponse>> GetCalendarForMemberAsync(Guid memberId, CalendarRequest request, CancellationToken ct = default)
    {
        var events = await _events.GetCalendarForMemberAsync(memberId, request.From, request.To, ct);
        return events.Select(e => e.ToCalendar()).ToList();
    }

    /// <inheritdoc/>
    public async Task<EventDetailResponse> GetByIdAsync(Guid eventId, CancellationToken ct = default)
    {
        var ev = await _events.GetWithDetailsAsync(eventId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zajęcia {eventId} nie istnieją.");
        return ev.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<EventDetailResponse> CreateAsync(
        Guid organizationId, CreateEventRequest request, Guid? creatorMemberId = null, CancellationToken ct = default)
    {
        var orgExists = await _organizations.ExistsAsync(organizationId, ct);
        if (!orgExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {organizationId} nie istnieje.");

        if (request.StartTime >= request.EndTime)
            throw new ServiceException(ServiceErrorCode.InvalidEventDateRange,
                "Data zakończenia musi być późniejsza niż data rozpoczęcia.", nameof(request.EndTime));

        NormalizeCreateParticipants(request.MemberIds, request.TeamIds, out var memberIds, out var teamIds);
        var groupId = request.EventType == EventType.IndividualSession ? null : request.GroupId;

        await ValidateLocationAndGroupAsync(organizationId, request.LocationId, groupId, ct);

        var entity = request.ToEntity(organizationId);
        entity.GroupId = groupId;
        var created = await _events.AddAsync(entity, ct);

        await MaybeAssignCreatorAsTrainerAsync(created.Id, creatorMemberId, ct);
        await AttachGroupParticipantsAsync(created.Id, groupId, ct);
        await AttachParticipantsAsync(created.Id, memberIds, teamIds, ct);

        var details = await _events.GetWithDetailsAsync(created.Id, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<CreateRecurringEventsResponse> CreateRecurringAsync(
        Guid organizationId, CreateRecurringEventsRequest request, Guid? creatorMemberId = null, CancellationToken ct = default)
    {
        var orgExists = await _organizations.ExistsAsync(organizationId, ct);
        if (!orgExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {organizationId} nie istnieje.");

        if (request.DateFrom > request.DateTo)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Data końcowa musi być późniejsza lub równa dacie początkowej.", nameof(request.DateTo));

        var rangeDays = request.DateTo.DayNumber - request.DateFrom.DayNumber;
        if (rangeDays > MaxRecurringRangeDays)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                $"Zakres dat nie może przekraczać {MaxRecurringRangeDays} dni.", nameof(request.DateTo));

        if (request.StartTime >= request.EndTime)
            throw new ServiceException(ServiceErrorCode.InvalidEventDateRange,
                "Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia.", nameof(request.EndTime));

        var daysOfWeek = request.DaysOfWeek.Distinct().ToHashSet();
        if (daysOfWeek.Count == 0)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Wybierz co najmniej jeden dzień tygodnia.", nameof(request.DaysOfWeek));

        NormalizeCreateParticipants(request.MemberIds, request.TeamIds, out var memberIds, out var teamIds);
        var groupId = request.EventType == EventType.IndividualSession ? null : request.GroupId;

        await ValidateLocationAndGroupAsync(organizationId, request.LocationId, groupId, ct);

        var seriesGroupId = Guid.NewGuid();
        var toCreate = new List<Event>();
        var currentDate = request.DateFrom;

        while (currentDate <= request.DateTo && toCreate.Count < MaxRecurringEvents)
        {
            if (daysOfWeek.Contains(currentDate.DayOfWeek))
            {
                var startUtc = new DateTime(
                    currentDate.Year, currentDate.Month, currentDate.Day,
                    request.StartTime.Hour, request.StartTime.Minute, 0,
                    DateTimeKind.Utc);

                var endUtc = new DateTime(
                    currentDate.Year, currentDate.Month, currentDate.Day,
                    request.EndTime.Hour, request.EndTime.Minute, 0,
                    DateTimeKind.Utc);

                toCreate.Add(new Event
                {
                    OrganizationId = organizationId,
                    SeriesGroupId  = seriesGroupId,
                    Title          = request.Title.Trim(),
                    Description    = request.Description?.Trim(),
                    StartTime      = startUtc,
                    EndTime        = endUtc,
                    LocationId     = request.LocationId,
                    GroupId        = groupId,
                    EventType      = request.EventType,
                    Color          = request.Color?.Trim(),
                    UnitCost       = request.UnitCost,
                    Currency       = request.Currency?.Trim().ToUpperInvariant(),
                    Status         = EventStatus.Scheduled,
                });
            }

            currentDate = currentDate.AddDays(1);
        }

        if (toCreate.Count == 0)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "W podanym zakresie nie ma żadnego dnia pasującego do wybranych dni tygodnia.");

        var created = await _events.AddRangeAsync(toCreate, ct);

        foreach (var ev in created)
        {
            await MaybeAssignCreatorAsTrainerAsync(ev.Id, creatorMemberId, ct);
            await AttachGroupParticipantsAsync(ev.Id, groupId, ct);
            await AttachParticipantsAsync(ev.Id, memberIds, teamIds, ct);
        }

        _logger.LogInformation(
            "Utworzono cykl {SeriesGroupId}: {Count} zajęć w organizacji {OrganizationId}.",
            seriesGroupId, created.Count, organizationId);

        return new CreateRecurringEventsResponse
        {
            SeriesGroupId  = seriesGroupId,
            GeneratedCount = created.Count,
            SkippedCount   = 0,
            EventIds       = created.OrderBy(e => e.StartTime).Select(e => e.Id).ToList(),
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EventSummaryResponse>> GetBySeriesGroupAsync(
        Guid organizationId, Guid seriesGroupId, CancellationToken ct = default)
    {
        var events = await _events.GetBySeriesGroupAsync(seriesGroupId, ct);
        var inOrg = events.Where(e => e.OrganizationId == organizationId).ToList();
        if (inOrg.Count == 0)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Cykl {seriesGroupId} nie istnieje w tej organizacji.");

        return inOrg.Select(e => e.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<CancelFutureInSeriesGroupResponse> CancelFutureInSeriesGroupAsync(
        Guid organizationId, Guid seriesGroupId, CancelFutureInSeriesGroupRequest request, CancellationToken ct = default)
    {
        var events = await _events.GetBySeriesGroupAsync(seriesGroupId, ct);
        var inOrg = events.Where(e => e.OrganizationId == organizationId).ToList();
        if (inOrg.Count == 0)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Cykl {seriesGroupId} nie istnieje w tej organizacji.");

        var now = DateTime.UtcNow;
        var toCancel = inOrg
            .Where(e => e.Status == EventStatus.Scheduled && e.StartTime >= now)
            .OrderBy(e => e.StartTime)
            .ToList();

        var cancelledCount = 0;
        foreach (var ev in toCancel)
        {
            // GetWithDetails for enrollments when notifying
            var detailed = request.NotifyParticipants
                ? await _events.GetWithDetailsAsync(ev.Id, ct) ?? ev
                : ev;

            detailed.Status = EventStatus.Cancelled;
            await _events.UpdateAsync(detailed, ct);
            cancelledCount++;

            if (request.NotifyParticipants && detailed.Enrollments.Any())
            {
                var recipientIds = detailed.Enrollments
                    .Where(e => e.Status == EventEnrollmentStatus.Enrolled)
                    .Select(e => e.OrganizationMemberId)
                    .Distinct()
                    .ToList();

                if (recipientIds.Count > 0)
                {
                    var reason = string.IsNullOrWhiteSpace(request.Reason)
                        ? "Brak podanego powodu."
                        : request.Reason;

                    try
                    {
                        await _messages.SendSystemMessageAsync(
                            detailed.OrganizationId,
                            $"Odwołanie zajęć: {detailed.Title}",
                            $"Zajęcia \"{detailed.Title}\" zaplanowane na {detailed.StartTime:dd.MM.yyyy HH:mm} zostały odwołane (odwołanie całego cyklu).\n\nPowód: {reason}",
                            recipientIds,
                            detailed.Id,
                            ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Nie udało się wysłać powiadomienia o odwołaniu zajęć {EventId} w cyklu {SeriesGroupId}.",
                            detailed.Id, seriesGroupId);
                    }
                }
            }
        }

        return new CancelFutureInSeriesGroupResponse
        {
            SeriesGroupId  = seriesGroupId,
            CancelledCount = cancelledCount,
        };
    }

    /// <inheritdoc/>
    public async Task<EventDetailResponse> UpdateAsync(Guid eventId, UpdateEventRequest request, CancellationToken ct = default)
    {
        var ev = await _events.GetByIdAsync(eventId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zajęcia {eventId} nie istnieją.");

        if (ev.Status == EventStatus.Cancelled)
            throw new ServiceException(ServiceErrorCode.EventCancelled,
                "Nie można edytować odwołanych zajęć.");

        if (ev.Status == EventStatus.Completed)
            throw new ServiceException(ServiceErrorCode.EventCompleted,
                "Nie można edytować zakończonych zajęć.");

        if (request.StartTime >= request.EndTime)
            throw new ServiceException(ServiceErrorCode.InvalidEventDateRange,
                "Data zakończenia musi być późniejsza niż data rozpoczęcia.", nameof(request.EndTime));

        // GroupId jest niezmienne po utworzeniu — null/pominięte w request = zostaw istniejące.
        var existingGroupId = ev.GroupId;
        if (request.GroupId.HasValue && request.GroupId != existingGroupId)
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Nie można zmieniać grupy przypisanej do zajęć.", nameof(request.GroupId));

        if (request.EventType == EventType.IndividualSession && existingGroupId.HasValue)
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Nie można zmienić typu na zajęcia indywidualne, gdy do zajęć przypisana jest grupa.",
                nameof(request.EventType));

        await ValidateLocationAndGroupAsync(ev.OrganizationId, request.LocationId, existingGroupId, ct);

        ev.ApplyUpdate(request);
        ev.GroupId = existingGroupId;
        await _events.UpdateAsync(ev, ct);

        var details = await _events.GetWithDetailsAsync(eventId, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<EventDetailResponse> CancelAsync(Guid eventId, CancelEventRequest request, CancellationToken ct = default)
    {
        var ev = await _events.GetWithDetailsAsync(eventId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zajęcia {eventId} nie istnieją.");

        if (ev.Status == EventStatus.Cancelled)
            throw new ServiceException(ServiceErrorCode.EventCancelled, "Zajęcia są już odwołane.");

        if (ev.Status == EventStatus.Completed)
            throw new ServiceException(ServiceErrorCode.EventCompleted, "Nie można odwołać zakończonych zajęć.");

        ev.Status = EventStatus.Cancelled;
        await _events.UpdateAsync(ev, ct);

        if (request.NotifyParticipants && ev.Enrollments.Any())
        {
            var recipientIds = ev.Enrollments
                .Where(e => e.Status == EventEnrollmentStatus.Enrolled)
                .Select(e => e.OrganizationMemberId)
                .Distinct()
                .ToList();

            if (recipientIds.Count > 0)
            {
                var reason = string.IsNullOrWhiteSpace(request.Reason)
                    ? "Brak podanego powodu."
                    : request.Reason;

                try
                {
                    await _messages.SendSystemMessageAsync(
                        ev.OrganizationId,
                        $"Odwołanie zajęć: {ev.Title}",
                        $"Zajęcia \"{ev.Title}\" zaplanowane na {ev.StartTime:dd.MM.yyyy HH:mm} zostały odwołane.\n\nPowód: {reason}",
                        recipientIds,
                        ev.Id,
                        ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Nie udało się wysłać powiadomienia o odwołaniu zajęć {EventId}.", eventId);
                }
            }
        }

        return ev.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<EventDetailResponse> CompleteAsync(Guid eventId, CancellationToken ct = default)
    {
        var ev = await _events.GetWithDetailsAsync(eventId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zajęcia {eventId} nie istnieją.");

        if (ev.Status == EventStatus.Cancelled)
            throw new ServiceException(ServiceErrorCode.EventCancelled, "Nie można zakończyć odwołanych zajęć.");

        if (ev.Status == EventStatus.Completed)
            throw new ServiceException(ServiceErrorCode.EventCompleted, "Zajęcia są już zakończone.");

        ev.Status = EventStatus.Completed;
        await _events.UpdateAsync(ev, ct);

        var activeEnrollments = ev.Enrollments.Where(e => e.Status == EventEnrollmentStatus.Enrolled).ToList();
        foreach (var enrollment in activeEnrollments)
        {
            enrollment.Status = EventEnrollmentStatus.Attended;
            await _enrollments.UpdateAsync(enrollment, ct);
        }

        _logger.LogInformation("Zakończono zajęcia {EventId}. Oznaczono {Count} uczestników jako Attended.",
            eventId, activeEnrollments.Count);

        var details = await _events.GetWithDetailsAsync(eventId, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid eventId, CancellationToken ct = default)
    {
        var ev = await _events.GetWithDetailsAsync(eventId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zajęcia {eventId} nie istnieją.");

        if (ev.Status != EventStatus.Scheduled)
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Można usuwać tylko zaplanowane zajęcia. Odwołaj je zamiast usuwać.");

        if (ev.Enrollments.Any(e => e.Status == EventEnrollmentStatus.Enrolled))
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Nie można usunąć zajęć z aktywnymi zapisami. Odwołaj zajęcia, aby poinformować uczestników.");

        await _events.DeleteAsync(eventId, ct);
    }

    /// <inheritdoc/>
    public async Task<EventDetailResponse> AssignTrainerAsync(Guid eventId, Guid trainerMemberId, CancellationToken ct = default)
    {
        var ev = await _events.GetByIdAsync(eventId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zajęcia {eventId} nie istnieją.");

        if (ev.Status == EventStatus.Cancelled)
            throw new ServiceException(ServiceErrorCode.EventCancelled, "Nie można edytować odwołanych zajęć.");

        var trainer = await _members.GetWithDetailsAsync(trainerMemberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Trener {trainerMemberId} nie istnieje.");

        if (!trainer.Roles.Any(r => r.Role == MemberRole.Trainer))
            throw new ServiceException(ServiceErrorCode.NotATrainer, "Wskazana osoba nie ma roli Trenera.");

        if (await _events.IsTrainerAssignedAsync(eventId, trainerMemberId, ct))
            throw new ServiceException(ServiceErrorCode.TrainerAlreadyAssignedToEvent,
                "Trener jest już przypisany do tych zajęć.");

        await _events.AddTrainerAsync(eventId, trainerMemberId, ct);

        var details = await _events.GetWithDetailsAsync(eventId, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<EventDetailResponse> RemoveTrainerAsync(Guid eventId, Guid trainerMemberId, CancellationToken ct = default)
    {
        var exists = await _events.ExistsAsync(eventId, ct);
        if (!exists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Zajęcia {eventId} nie istnieją.");

        if (!await _events.IsTrainerAssignedAsync(eventId, trainerMemberId, ct))
            throw new ServiceException(ServiceErrorCode.NotFound,
                "Trener nie jest przypisany do tych zajęć.");

        await _events.RemoveTrainerAsync(eventId, trainerMemberId, ct);

        var details = await _events.GetWithDetailsAsync(eventId, ct);
        return details!.ToDetail();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void NormalizeCreateParticipants(
        IReadOnlyList<Guid> memberIds,
        IReadOnlyList<Guid> teamIds,
        out List<Guid> normalizedMembers,
        out List<Guid> normalizedTeams)
    {
        normalizedMembers = memberIds.Where(id => id != Guid.Empty).Distinct().ToList();
        normalizedTeams   = teamIds.Where(id => id != Guid.Empty).Distinct().ToList();

        if (normalizedMembers.Count > MaxCreateMembers)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                $"Można zapisać maksymalnie {MaxCreateMembers} uczestników przy tworzeniu zajęć.", nameof(memberIds));

        if (normalizedTeams.Count > MaxCreateTeams)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                $"Można zapisać maksymalnie {MaxCreateTeams} zespołów przy tworzeniu zajęć.", nameof(teamIds));
    }

    private async Task MaybeAssignCreatorAsTrainerAsync(Guid eventId, Guid? creatorMemberId, CancellationToken ct)
    {
        if (!creatorMemberId.HasValue) return;

        var member = await _members.GetWithRolesAsync(creatorMemberId.Value, ct);
        if (member is null) return;
        if (!member.Roles.Any(r => r.Role == MemberRole.Trainer)) return;
        if (await _events.IsTrainerAssignedAsync(eventId, creatorMemberId.Value, ct)) return;

        await _events.AddTrainerAsync(eventId, creatorMemberId.Value, ct);
    }

    private async Task AttachGroupParticipantsAsync(Guid eventId, Guid? groupId, CancellationToken ct)
    {
        if (!groupId.HasValue) return;

        var group = await _groups.GetWithDetailsAsync(groupId.Value, ct);
        if (group is null) return;

        var teamIds = group.Teams
            .Where(tg => tg.Team is { IsActive: true })
            .Select(tg => tg.TeamId)
            .Distinct()
            .ToList();

        var memberIds = group.Members
            .Where(gm => gm.OrganizationMember is { IsActive: true })
            .Select(gm => gm.OrganizationMemberId)
            .Distinct()
            .ToList();

        // Zespoły grupy + bezpośredni członkowie grupy (duplikaty członków z zespołów są pomijane).
        await _enrollmentService.EnrollTeamsOnCreateAsync(eventId, teamIds, ct);
        await _enrollmentService.EnrollMembersOnCreateAsync(eventId, memberIds, ct);
    }

    private async Task AttachParticipantsAsync(
        Guid eventId, IReadOnlyList<Guid> memberIds, IReadOnlyList<Guid> teamIds, CancellationToken ct)
    {
        // Najpierw zespoły (tworzą też indywidualne zapisy członków), potem pozostali uczestnicy.
        await _enrollmentService.EnrollTeamsOnCreateAsync(eventId, teamIds, ct);
        await _enrollmentService.EnrollMembersOnCreateAsync(eventId, memberIds, ct);
    }

    private async Task ValidateLocationAndGroupAsync(
        Guid organizationId, Guid? locationId, Guid? groupId, CancellationToken ct)
    {
        if (locationId.HasValue)
        {
            var loc = await _locations.GetByIdAsync(locationId.Value, ct);
            if (loc is null || loc.OrganizationId != organizationId)
                throw new ServiceException(ServiceErrorCode.ValidationError,
                    "Wskazana lokalizacja nie należy do tej organizacji.", nameof(locationId));
        }

        if (groupId.HasValue)
        {
            var group = await _groups.GetByIdAsync(groupId.Value, ct);
            if (group is null || group.OrganizationId != organizationId)
                throw new ServiceException(ServiceErrorCode.ValidationError,
                    "Wskazana grupa nie należy do tej organizacji.", nameof(groupId));
        }
    }
}
