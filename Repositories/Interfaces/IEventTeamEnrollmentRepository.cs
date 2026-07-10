using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium zapisów całych zespołów na zajęcia (EventTeamEnrollment).
/// </summary>
public interface IEventTeamEnrollmentRepository : IBaseRepository<EventTeamEnrollment>
{
    /// <summary>
    /// Pobiera zapis zespołu na konkretne zajęcia.
    /// Zwraca null jeśli zapis nie istnieje.
    /// </summary>
    Task<EventTeamEnrollment?> GetByEventAndTeamAsync(Guid eventId, Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera zapis z pełnymi danymi — Event, Team (z członkami).
    /// </summary>
    Task<EventTeamEnrollment?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę zapisów zespołów z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<EventTeamEnrollment>> GetPagedAsync(EventTeamEnrollmentFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie zapisy zespołów na konkretne zajęcia.
    /// </summary>
    Task<IReadOnlyList<EventTeamEnrollment>> GetByEventAsync(Guid eventId, EventEnrollmentStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie zapisy danego zespołu (historia zajęć zespołu).
    /// </summary>
    Task<IReadOnlyList<EventTeamEnrollment>> GetByTeamAsync(Guid teamId, EventEnrollmentStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy zespół jest już zapisany na dane zajęcia.
    /// </summary>
    Task<bool> IsEnrolledAsync(Guid eventId, Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zmienia status zapisu zespołu. Zwraca false jeśli zapis nie istnieje.
    /// </summary>
    Task<bool> SetStatusAsync(Guid enrollmentId, EventEnrollmentStatus status, CancellationToken cancellationToken = default);
}
