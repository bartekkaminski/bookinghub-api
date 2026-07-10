using BookingHub.Api.Dtos.Event;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class EventMappings
{
    private const string DefaultColor = "#9CA3AF";

    /// <summary>
    /// Wyznacza kolor zdarzenia: własny → serii → grupy → domyślny szary.
    /// </summary>
    public static string ResolveColor(this Event ev) =>
        !string.IsNullOrWhiteSpace(ev.Color)                    ? ev.Color :
        !string.IsNullOrWhiteSpace(ev.EventSeries?.DefaultColor) ? ev.EventSeries.DefaultColor :
        !string.IsNullOrWhiteSpace(ev.Group?.Color)              ? ev.Group.Color :
        DefaultColor;

    public static EventSummaryResponse ToSummary(this Event ev) => new()
    {
        Id             = ev.Id,
        OrganizationId = ev.OrganizationId,
        Title          = ev.Title,
        StartTime      = ev.StartTime,
        EndTime        = ev.EndTime,
        EventType      = ev.EventType,
        Status         = ev.Status,
        Color          = ev.Color,
        LocationId     = ev.LocationId,
        LocationName   = ev.Location?.Name,
        GroupId        = ev.GroupId,
        GroupName      = ev.Group?.Name,
        EventSeriesId  = ev.EventSeriesId,
        EnrolledCount  = ev.Enrollments.Count(e => e.Status == EventEnrollmentStatus.Enrolled),
        Trainers       = ev.Trainers.Select(t => new EventTrainerInfo
        {
            MemberId    = t.OrganizationMemberId,
            DisplayName = t.OrganizationMember?.ResolveDisplayName() ?? string.Empty,
            Color       = t.OrganizationMember?.Color,
            PhotoUrl    = t.OrganizationMember?.PhotoUrl ?? t.OrganizationMember?.Person?.PhotoUrl,
        }).ToList(),
    };

    public static EventCalendarResponse ToCalendar(this Event ev) => new()
    {
        Id            = ev.Id,
        Title         = ev.Title,
        StartTime     = ev.StartTime,
        EndTime       = ev.EndTime,
        EventType     = ev.EventType,
        Status        = ev.Status,
        Color         = ev.ResolveColor(),
        LocationName  = ev.Location?.Name,
        GroupName     = ev.Group?.Name,
        TrainerNames  = ev.Trainers.Select(t => t.OrganizationMember?.ResolveDisplayName() ?? string.Empty).ToList(),
        EnrolledCount = ev.Enrollments.Count(e => e.Status == EventEnrollmentStatus.Enrolled),
        EventSeriesId = ev.EventSeriesId,
    };

    public static EventDetailResponse ToDetail(this Event ev) => new()
    {
        Id              = ev.Id,
        OrganizationId  = ev.OrganizationId,
        EventSeriesId   = ev.EventSeriesId,
        Title           = ev.Title,
        Description     = ev.Description,
        StartTime       = ev.StartTime,
        EndTime         = ev.EndTime,
        EventType       = ev.EventType,
        Status          = ev.Status,
        Color           = ev.Color,
        UnitCost        = ev.UnitCost,
        Currency        = ev.Currency,
        LocationId      = ev.LocationId,
        LocationName    = ev.Location?.Name,
        LocationAddress = ev.Location?.Address,
        GroupId         = ev.GroupId,
        GroupName       = ev.Group?.Name,
        Trainers        = ev.Trainers.Select(t => new EventTrainerInfo
        {
            MemberId    = t.OrganizationMemberId,
            DisplayName = t.OrganizationMember?.ResolveDisplayName() ?? string.Empty,
            Color       = t.OrganizationMember?.Color,
            PhotoUrl    = t.OrganizationMember?.PhotoUrl ?? t.OrganizationMember?.Person?.PhotoUrl,
        }).ToList(),
        Enrollments = ev.Enrollments.Select(e => new EventEnrollmentInfo
        {
            EnrollmentId            = e.Id,
            MemberId                = e.OrganizationMemberId,
            DisplayName             = e.OrganizationMember?.ResolveDisplayName() ?? string.Empty,
            PhotoUrl                = e.OrganizationMember?.PhotoUrl ?? e.OrganizationMember?.Person?.PhotoUrl,
            Status                  = e.Status.ToString(),
            HasPendingCancellation  = e.CancellationRequests.Any(cr => cr.Status == CancellationStatus.Pending),
        }).ToList(),
        TeamEnrollments = ev.TeamEnrollments.Select(te => new EventTeamEnrollmentInfo
        {
            EnrollmentId = te.Id,
            TeamId       = te.TeamId,
            TeamName     = te.Team?.Name,
            Status       = te.Status.ToString(),
            MemberNames  = te.Team?.Members.Select(tm => tm.OrganizationMember?.ResolveDisplayName() ?? string.Empty).ToList() ?? [],
        }).ToList(),
        CreatedAt = ev.CreatedAt,
        UpdatedAt = ev.UpdatedAt,
    };

    public static Event ToEntity(this CreateEventRequest dto, Guid organizationId) => new()
    {
        OrganizationId = organizationId,
        Title          = dto.Title.Trim(),
        Description    = dto.Description?.Trim(),
        StartTime      = dto.StartTime.ToUniversalTime(),
        EndTime        = dto.EndTime.ToUniversalTime(),
        LocationId     = dto.LocationId,
        GroupId        = dto.GroupId,
        EventSeriesId  = dto.EventSeriesId,
        EventType      = dto.EventType,
        Color          = dto.Color?.Trim(),
        UnitCost       = dto.UnitCost,
        Currency       = dto.Currency?.Trim().ToUpperInvariant(),
        Status         = EventStatus.Scheduled,
    };

    public static void ApplyUpdate(this Event ev, UpdateEventRequest dto)
    {
        ev.Title       = dto.Title.Trim();
        ev.Description = dto.Description?.Trim();
        ev.StartTime   = dto.StartTime.ToUniversalTime();
        ev.EndTime     = dto.EndTime.ToUniversalTime();
        ev.LocationId  = dto.LocationId;
        ev.GroupId     = dto.GroupId;
        ev.EventType   = dto.EventType;
        ev.Color       = dto.Color?.Trim();
        ev.UnitCost    = dto.UnitCost;
        ev.Currency    = dto.Currency?.Trim().ToUpperInvariant();
    }
}
