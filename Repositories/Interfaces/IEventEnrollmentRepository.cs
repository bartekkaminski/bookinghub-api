using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium indywidualnych zapisów uczestników na zajęcia (EventEnrollment).
/// </summary>
public interface IEventEnrollmentRepository : IBaseRepository<EventEnrollment>
{
    /// <summary>
    /// Pobiera zapis uczestnika na konkretne zajęcia.
    /// Zwraca null jeśli zapis nie istnieje.
    /// </summary>
    Task<EventEnrollment?> GetByEventAndMemberAsync(Guid eventId, Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera zapis wraz z historią wniosków o odwołanie (CancellationRequests).
    /// </summary>
    Task<EventEnrollment?> GetWithCancellationRequestsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera zapis z pełnymi danymi — Event, OrganizationMember, CancellationRequests.
    /// </summary>
    Task<EventEnrollment?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę zapisów z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<EventEnrollment>> GetPagedAsync(EventEnrollmentFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie zapisy na konkretne zajęcia (lista uczestników).
    /// </summary>
    Task<IReadOnlyList<EventEnrollment>> GetByEventAsync(Guid eventId, EventEnrollmentStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie zapisy uczestnika (historia jego zajęć).
    /// </summary>
    Task<IReadOnlyList<EventEnrollment>> GetByMemberAsync(Guid organizationMemberId, EventEnrollmentStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy uczestnik jest już zapisany na dane zajęcia (status Enrolled lub Attended).
    /// </summary>
    Task<bool> IsEnrolledAsync(Guid eventId, Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zmienia status zapisu. Zwraca false jeśli zapis nie istnieje.
    /// </summary>
    Task<bool> SetStatusAsync(Guid enrollmentId, EventEnrollmentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera liczbę aktywnych zapisów (Enrolled) na konkretne zajęcia.
    /// </summary>
    Task<int> GetEnrolledCountAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną historię zapisów uczestnika.
    /// </summary>
    Task<PagedResult<EventEnrollment>> GetByMemberPagedAsync(Guid organizationMemberId, EventEnrollmentFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera aktywny zapis uczestnika na zajęcia (status = Enrolled).
    /// Zwraca null jeśli nie istnieje lub jest anulowany.
    /// </summary>
    Task<EventEnrollment?> GetActiveByEventAndMemberAsync(Guid eventId, Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera zapisy uczestnika ze statusem Attended w danym przedziale dat.
    /// Używane do kalkulacji rozliczeń.
    /// </summary>
    Task<IReadOnlyList<EventEnrollment>> GetAttendedByMemberInPeriodAsync(Guid organizationMemberId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masowo ustawia obecność/nieobecność na zajęciach (zmienia Enrolled → Attended/Absent).
    /// </summary>
    Task BulkSetAttendanceAsync(Guid eventId, IEnumerable<Guid> presentMemberIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera zapisy uczestnika w danym przedziale dat (do kalkulacji rozliczeń).
    /// </summary>
    Task<IReadOnlyList<EventEnrollment>> GetByMemberInRangeAsync(Guid organizationMemberId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
