using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium wniosków o odwołanie zapisu na zajęcia (CancellationRequest).
/// </summary>
public interface ICancellationRequestRepository : IBaseRepository<CancellationRequest>
{
    /// <summary>
    /// Pobiera wniosek z pełnymi danymi — EventEnrollment (Event, OrganizationMember), RequestedBy, ReviewedBy.
    /// </summary>
    Task<CancellationRequest?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę wniosków z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<CancellationRequest>> GetPagedAsync(CancellationRequestFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie wnioski dla danego zapisu na zajęcia (historia wniosków).
    /// </summary>
    Task<IReadOnlyList<CancellationRequest>> GetByEnrollmentAsync(Guid eventEnrollmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie oczekujące wnioski złożone przez uczestnika.
    /// </summary>
    Task<IReadOnlyList<CancellationRequest>> GetPendingByMemberAsync(Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie oczekujące wnioski w organizacji (do obsługi przez trenera / admina).
    /// </summary>
    Task<IReadOnlyList<CancellationRequest>> GetPendingByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy dla danego zapisu istnieje już oczekujący wniosek.
    /// </summary>
    Task<bool> HasPendingRequestAsync(Guid eventEnrollmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rozpatruje wniosek — ustawia Status, ReviewedByPersonId, ReviewedAt, ReviewNote.
    /// Zwraca false jeśli wniosek nie istnieje.
    /// </summary>
    Task<bool> ReviewAsync(Guid requestId, CancellationStatus decision, Guid reviewedByPersonId, string? reviewNote, CancellationToken cancellationToken = default);
}
