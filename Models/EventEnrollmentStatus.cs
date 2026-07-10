namespace BookingHub.Api.Models;

public enum EventEnrollmentStatus
{
    /// <summary>Oczekuje na zatwierdzenie przez trenera/admina.</summary>
    PendingApproval,
    Enrolled,
    Cancelled,
    Attended,
    Absent
}
