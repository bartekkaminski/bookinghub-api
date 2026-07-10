using BookingHub.Api.Dtos.EventSeries;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania seriami cyklicznych zajęć.
/// </summary>
public sealed class EventSeriesService : IEventSeriesService
{
    private readonly IEventSeriesRepository _series;
    private readonly IOrganizationRepository _organizations;

    public EventSeriesService(
        IEventSeriesRepository series,
        IOrganizationRepository organizations)
    {
        _series        = series;
        _organizations = organizations;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<EventSeriesSummaryResponse>> GetPagedAsync(Guid organizationId, EventSeriesFilterParams filter, CancellationToken ct = default)
    {
        filter.OrganizationId = organizationId;
        var paged = await _series.GetPagedAsync(filter, ct);
        return paged.Map(s => s.ToSummary());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EventSeriesSummaryResponse>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var all = await _series.GetByOrganizationAsync(organizationId, onlyActive: true, ct);
        return all.Select(s => s.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<EventSeriesDetailResponse> GetByIdAsync(Guid seriesId, CancellationToken ct = default)
    {
        var series = await _series.GetWithDetailsAsync(seriesId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Seria {seriesId} nie istnieje.");
        return series.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<EventSeriesDetailResponse> CreateAsync(Guid organizationId, CreateEventSeriesRequest request, CancellationToken ct = default)
    {
        var orgExists = await _organizations.ExistsAsync(organizationId, ct);
        if (!orgExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {organizationId} nie istnieje.");

        var entity  = request.ToEntity(organizationId);
        var created = await _series.AddAsync(entity, ct);
        var details = await _series.GetWithDetailsAsync(created.Id, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<EventSeriesDetailResponse> UpdateAsync(Guid seriesId, UpdateEventSeriesRequest request, CancellationToken ct = default)
    {
        var series = await _series.GetByIdAsync(seriesId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Seria {seriesId} nie istnieje.");

        series.ApplyUpdate(request);
        await _series.UpdateAsync(series, ct);

        var details = await _series.GetWithDetailsAsync(seriesId, ct);
        return details!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid seriesId, CancellationToken ct = default)
    {
        var exists = await _series.ExistsAsync(seriesId, ct);
        if (!exists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Seria {seriesId} nie istnieje.");

        await _series.DeleteAsync(seriesId, ct);
    }
}
