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
    private readonly IEventRepository _events;
    private readonly IOrganizationRepository _organizations;
    private readonly IOrganizationMemberRepository _members;
    private readonly IEventEnrollmentRepository _enrollments;
    private readonly ILocationRepository _locations;
    private readonly IGroupRepository _groups;
    private readonly IEventSeriesRepository _series;
    private readonly IMessageService _messages;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IEventRepository events,
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members,
        IEventEnrollmentRepository enrollments,
        ILocationRepository locations,
        IGroupRepository groups,
        IEventSeriesRepository series,
        IMessageService messages,
        ILogger<EventService> logger)
    {
        _events        = events;
        _organizations = organizations;
        _members       = members;
        _enrollments   = enrollments;
        _locations     = locations;
        _groups        = groups;
        _series        = series;
        _messages      = messages;
        _logger        = logger;
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
    public async Task<EventDetailResponse> CreateAsync(Guid organizationId, CreateEventRequest request, CancellationToken ct = default)
    {
        var orgExists = await _organizations.ExistsAsync(organizationId, ct);
        if (!orgExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {organizationId} nie istnieje.");

        if (request.StartTime >= request.EndTime)
            throw new ServiceException(ServiceErrorCode.InvalidEventDateRange,
                "Data zakończenia musi być późniejsza niż data rozpoczęcia.", nameof(request.EndTime));

        // Walidacja FK — zasoby muszą należeć do tej samej organizacji.
        if (request.LocationId.HasValue)
        {
            var loc = await _locations.GetByIdAsync(request.LocationId.Value, ct);
            if (loc is null || loc.OrganizationId != organizationId)
                throw new ServiceException(ServiceErrorCode.ValidationError,
                    "Wskazana lokalizacja nie należy do tej organizacji.", nameof(request.LocationId));
        }
        if (request.GroupId.HasValue)
        {
            var group = await _groups.GetByIdAsync(request.GroupId.Value, ct);
            if (group is null || group.OrganizationId != organizationId)
                throw new ServiceException(ServiceErrorCode.ValidationError,
                    "Wskazana grupa nie należy do tej organizacji.", nameof(request.GroupId));
        }
        if (request.EventSeriesId.HasValue)
        {
            var s = await _series.GetByIdAsync(request.EventSeriesId.Value, ct);
            if (s is null || s.OrganizationId != organizationId)
                throw new ServiceException(ServiceErrorCode.ValidationError,
                    "Wskazana seria nie należy do tej organizacji.", nameof(request.EventSeriesId));
        }

        var entity  = request.ToEntity(organizationId);
        var created = await _events.AddAsync(entity, ct);

        // Przypisz trenerów jeśli wskazano
        foreach (var trainerId in request.TrainerMemberIds)
        {
            var trainer = await _members.GetByIdAsync(trainerId, ct);
            if (trainer is not null && trainer.Roles.Any(r => r.Role == MemberRole.Trainer))
                await _events.AddTrainerAsync(created.Id, trainerId, ct);
        }

        var details = await _events.GetWithDetailsAsync(created.Id, ct);
        return details!.ToDetail();
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

        // Walidacja FK przy update — zasoby muszą należeć do tej samej organizacji.
        if (request.LocationId.HasValue)
        {
            var loc = await _locations.GetByIdAsync(request.LocationId.Value, ct);
            if (loc is null || loc.OrganizationId != ev.OrganizationId)
                throw new ServiceException(ServiceErrorCode.ValidationError,
                    "Wskazana lokalizacja nie należy do tej organizacji.", nameof(request.LocationId));
        }
        if (request.GroupId.HasValue)
        {
            var group = await _groups.GetByIdAsync(request.GroupId.Value, ct);
            if (group is null || group.OrganizationId != ev.OrganizationId)
                throw new ServiceException(ServiceErrorCode.ValidationError,
                    "Wskazana grupa nie należy do tej organizacji.", nameof(request.GroupId));
        }

        ev.ApplyUpdate(request);
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

        // Wyślij powiadomienie do uczestników
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

        // Oznacz wszystkie aktywne zapisy jako Attended
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
}
