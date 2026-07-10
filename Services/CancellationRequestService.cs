using BookingHub.Api.Dtos.Cancellation;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania wnioskami o odwołanie zapisu na zajęcia.
/// </summary>
public sealed class CancellationRequestService : ICancellationRequestService
{
    private readonly ICancellationRequestRepository _requests;
    private readonly IEventEnrollmentRepository _enrollments;
    private readonly ILogger<CancellationRequestService> _logger;

    public CancellationRequestService(
        ICancellationRequestRepository requests,
        IEventEnrollmentRepository enrollments,
        ILogger<CancellationRequestService> logger)
    {
        _requests    = requests;
        _enrollments = enrollments;
        _logger      = logger;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<CancellationRequestSummaryResponse>> GetPagedAsync(Guid organizationId, CancellationRequestFilterParams filter, CancellationToken ct = default)
    {
        filter.OrganizationId = organizationId;
        var paged = await _requests.GetPagedAsync(filter, ct);
        return paged.Map(cr => cr.ToSummary());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CancellationRequestSummaryResponse>> GetPendingForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var pending = await _requests.GetPendingByOrganizationAsync(organizationId, ct);
        return pending.Select(cr => cr.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CancellationRequestSummaryResponse>> GetByMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        var requests = await _requests.GetPendingByMemberAsync(memberId, ct);
        return requests.Select(cr => cr.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<CancellationRequestDetailResponse> GetByIdAsync(Guid requestId, CancellationToken ct = default)
    {
        var request = await _requests.GetWithDetailsAsync(requestId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Wniosek {requestId} nie istnieje.");
        return request.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<CancellationRequestDetailResponse> RequestAsync(Guid enrollmentId, Guid requestingMemberId, CreateCancellationRequest request, CancellationToken ct = default)
    {
        var enrollment = await _enrollments.GetByIdAsync(enrollmentId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Zapis {enrollmentId} nie istnieje.");

        if (enrollment.Status == EventEnrollmentStatus.Cancelled)
            throw new ServiceException(ServiceErrorCode.EnrollmentNotActive, "Zapis jest już anulowany.");

        if (enrollment.OrganizationMemberId != requestingMemberId)
            throw new ServiceException(ServiceErrorCode.Forbidden,
                "Możesz składać wnioski tylko dla własnych zapisów.");

        var hasExisting = await _requests.HasPendingRequestAsync(enrollmentId, ct);
        if (hasExisting)
            throw new ServiceException(ServiceErrorCode.CancellationRequestAlreadyPending,
                "Istnieje już oczekujący wniosek o odwołanie dla tego zapisu.");

        var entity = new CancellationRequest
        {
            EventEnrollmentId  = enrollmentId,
            RequestedByMemberId= requestingMemberId,
            Reason             = request.Reason?.Trim(),
            RequestedAt        = DateTime.UtcNow,
            Status             = CancellationStatus.Pending,
        };

        var created = await _requests.AddAsync(entity, ct);
        var details = await _requests.GetWithDetailsAsync(created.Id, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<CancellationRequestDetailResponse> ReviewAsync(Guid requestId, Guid reviewerPersonId, ReviewCancellationRequest request, CancellationToken ct = default)
    {
        if (request.Decision != CancellationStatus.Approved && request.Decision != CancellationStatus.Rejected)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Decyzja musi być Approved lub Rejected.", nameof(request.Decision));

        var cancellation = await _requests.GetWithDetailsAsync(requestId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Wniosek {requestId} nie istnieje.");

        if (cancellation.Status != CancellationStatus.Pending)
            throw new ServiceException(ServiceErrorCode.CancellationRequestNotPending,
                "Wniosek nie jest w stanie Pending — nie można go ponownie rozpatrzyć.");

        cancellation.Status           = request.Decision;
        cancellation.ReviewedByPersonId = reviewerPersonId;
        cancellation.ReviewedAt       = DateTime.UtcNow;
        cancellation.ReviewNote       = request.ReviewNote?.Trim();

        await _requests.UpdateAsync(cancellation, ct);

        // Przy Approved — anuluj zapis uczestnika
        if (request.Decision == CancellationStatus.Approved)
        {
            var enrollment = await _enrollments.GetByIdAsync(cancellation.EventEnrollmentId, ct);
            if (enrollment is not null && enrollment.Status == EventEnrollmentStatus.Enrolled)
            {
                enrollment.Status = EventEnrollmentStatus.Cancelled;
                await _enrollments.UpdateAsync(enrollment, ct);
                _logger.LogInformation(
                    "Wniosek {RequestId} zatwierdzony — zapis {EnrollmentId} anulowany.",
                    requestId, cancellation.EventEnrollmentId);
            }
        }

        var refreshed = await _requests.GetWithDetailsAsync(requestId, ct);
        return refreshed!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task WithdrawAsync(Guid requestId, CancellationToken ct = default)
    {
        var cancellation = await _requests.GetByIdAsync(requestId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Wniosek {requestId} nie istnieje.");

        if (cancellation.Status != CancellationStatus.Pending)
            throw new ServiceException(ServiceErrorCode.CancellationRequestNotPending,
                "Można wycofać tylko oczekujące wnioski.");

        cancellation.Status = CancellationStatus.Withdrawn;
        await _requests.UpdateAsync(cancellation, ct);
    }
}
