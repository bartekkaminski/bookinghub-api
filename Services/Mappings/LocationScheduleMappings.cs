using BookingHub.Api.Dtos.Location;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class LocationScheduleMappings
{
    /// <summary>
    /// Mapuje Event na LocationDayEventResponse dla widoku dziennego harmonogramu sali.
    /// Indywidualni uczestnicy zwracani są jako liczba (bez imion — prywatność).
    /// </summary>
    public static LocationDayEventResponse ToLocationDayEvent(this Event ev) => new()
    {
        Id        = ev.Id,
        Title     = ev.Title,
        StartTime = ev.StartTime,
        EndTime   = ev.EndTime,
        EventType = ev.EventType,
        Status    = ev.Status,
        Color     = ev.ResolveColor(),
        GroupId   = ev.GroupId,
        GroupName = ev.Group?.Name,

        IndividualCount = ev.Enrollments.Count(e =>
            e.Status is EventEnrollmentStatus.Enrolled
                     or EventEnrollmentStatus.PendingApproval
                     or EventEnrollmentStatus.Attended),

        Teams = ev.TeamEnrollments
            .Where(te => te.Status != EventEnrollmentStatus.Cancelled)
            .Select(te => new LocationDayTeamInfo
            {
                TeamId      = te.TeamId,
                TeamName    = te.Team?.Name,
                MemberCount = te.Team?.Members.Count ?? 0,
            })
            .ToList(),
    };
}
