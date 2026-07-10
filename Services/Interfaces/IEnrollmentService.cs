using BookingHub.Api.Dtos.Enrollment;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania zapisami na zajęcia (indywidualnymi i zespołowymi).
/// </summary>
public interface IEnrollmentService
{
    /// <summary>Pobiera stronicowaną listę zapisów indywidualnych dla zajęć.</summary>
    Task<PagedResult<EnrollmentSummaryResponse>> GetPagedForEventAsync(Guid eventId, EventEnrollmentFilterParams filter, CancellationToken ct = default);

    /// <summary>Pobiera historię zapisów danego uczestnika.</summary>
    Task<PagedResult<EnrollmentSummaryResponse>> GetPagedForMemberAsync(Guid memberId, EventEnrollmentFilterParams filter, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły zapisu indywidualnego.</summary>
    Task<EnrollmentDetailResponse> GetByIdAsync(Guid enrollmentId, CancellationToken ct = default);

    /// <summary>
    /// Zapisuje uczestnika na zajęcia.
    /// Rzuca wyjątek jeśli uczestnik jest już zapisany lub zajęcia są odwołane.
    /// </summary>
    Task<EnrollmentDetailResponse> EnrollMemberAsync(Guid eventId, Guid organizationMemberId, CancellationToken ct = default);

    /// <summary>
    /// Zapisuje cały zespół na zajęcia (tworzy EventTeamEnrollment + EventEnrollment dla każdego członka zespołu).
    /// </summary>
    Task<TeamEnrollmentSummaryResponse> EnrollTeamAsync(Guid eventId, Guid teamId, CancellationToken ct = default);

    /// <summary>
    /// Wypisuje uczestnika z zajęć (zmiana statusu na Cancelled).
    /// Może być wywołane przez uczestnika lub admina/trenera.
    /// </summary>
    Task UnenrollMemberAsync(Guid enrollmentId, CancellationToken ct = default);

    /// <summary>Wypisuje cały zespół z zajęć.</summary>
    Task UnenrollTeamAsync(Guid teamEnrollmentId, CancellationToken ct = default);

    /// <summary>Pobiera listę zapisów zespołów dla zajęć.</summary>
    Task<IReadOnlyList<TeamEnrollmentSummaryResponse>> GetTeamEnrollmentsForEventAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>Zmienia status zapisu (np. po zakończeniu zajęć).</summary>
    Task<EnrollmentDetailResponse> SetStatusAsync(Guid enrollmentId, BookingHub.Api.Models.EventEnrollmentStatus status, CancellationToken ct = default);

    /// <summary>Zbiorowe oznaczanie obecności dla wszystkich podanych zapisów.</summary>
    Task BulkMarkAttendedAsync(Guid eventId, BulkAttendanceRequest request, CancellationToken ct = default);
}
