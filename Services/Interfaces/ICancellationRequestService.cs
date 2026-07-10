using BookingHub.Api.Dtos.Cancellation;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania wnioskami o odwołanie zapisu na zajęcia.
/// </summary>
public interface ICancellationRequestService
{
    /// <summary>Pobiera stronicowaną listę wniosków (admin / trener widzi dla swojej org).</summary>
    Task<PagedResult<CancellationRequestSummaryResponse>> GetPagedAsync(Guid organizationId, CancellationRequestFilterParams filter, CancellationToken ct = default);

    /// <summary>Pobiera oczekujące wnioski dla organizacji (do obsługi przez trenera / admina).</summary>
    Task<IReadOnlyList<CancellationRequestSummaryResponse>> GetPendingForOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Pobiera wnioski złożone przez danego uczestnika.</summary>
    Task<IReadOnlyList<CancellationRequestSummaryResponse>> GetByMemberAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły wniosku.</summary>
    Task<CancellationRequestDetailResponse> GetByIdAsync(Guid requestId, CancellationToken ct = default);

    /// <summary>
    /// Uczestnik składa wniosek o odwołanie swojego zapisu.
    /// Rzuca wyjątek jeśli istnieje już oczekujący wniosek dla tego zapisu.
    /// </summary>
    Task<CancellationRequestDetailResponse> RequestAsync(Guid enrollmentId, Guid requestingMemberId, CreateCancellationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Trener / admin rozpatruje wniosek (Approved lub Rejected).
    /// Przy Approved: zmienia status zapisu na Cancelled.
    /// </summary>
    Task<CancellationRequestDetailResponse> ReviewAsync(Guid requestId, Guid reviewerPersonId, ReviewCancellationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Uczestnik lub admin cofa wniosek (zmienia status na Withdrawn).
    /// Możliwe tylko gdy status = Pending.
    /// </summary>
    Task WithdrawAsync(Guid requestId, CancellationToken ct = default);
}
