using System.Text.RegularExpressions;
using BookingHub.Api.Dtos.EventSeries;
using BookingHub.Api.Models;
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
    private readonly IEventRepository _events;

    private static readonly Dictionary<string, DayOfWeek> DayCodeMap = new()
    {
        ["MO"] = DayOfWeek.Monday,
        ["TU"] = DayOfWeek.Tuesday,
        ["WE"] = DayOfWeek.Wednesday,
        ["TH"] = DayOfWeek.Thursday,
        ["FR"] = DayOfWeek.Friday,
        ["SA"] = DayOfWeek.Saturday,
        ["SU"] = DayOfWeek.Sunday,
    };

    public EventSeriesService(
        IEventSeriesRepository series,
        IOrganizationRepository organizations,
        IEventRepository events)
    {
        _series        = series;
        _organizations = organizations;
        _events        = events;
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

    /// <inheritdoc/>
    public async Task<GenerateEventsResponse> GenerateEventsAsync(
        Guid organizationId, Guid seriesId, GenerateEventsRequest request, CancellationToken ct = default)
    {
        var series = await _series.GetByIdAsync(seriesId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Seria {seriesId} nie istnieje.");

        if (series.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Seria {seriesId} nie istnieje w tej organizacji.");

        if (!series.IsActive)
            throw new ServiceException(ServiceErrorCode.ValidationError, "Nie można generować zajęć dla nieaktywnej serii.");

        if (request.DateFrom > request.DateTo)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Data końcowa musi być późniejsza lub równa dacie początkowej.", nameof(request.DateTo));

        var rangeDays = request.DateTo.DayNumber - request.DateFrom.DayNumber;
        if (rangeDays > 365)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Zakres dat nie może przekraczać 365 dni.", nameof(request.DateTo));

        if (request.StartTime >= request.EndTime)
            throw new ServiceException(ServiceErrorCode.InvalidEventDateRange,
                "Godzina zakończenia musi być późniejsza niż godzina rozpoczęcia.", nameof(request.EndTime));

        if (string.IsNullOrWhiteSpace(series.RecurrenceRule))
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Seria nie ma zdefiniowanej reguły cykliczności. Najpierw edytuj serię i ustaw dni tygodnia.");

        var daysOfWeek = ParseRruleDays(series.RecurrenceRule);
        if (daysOfWeek.Count == 0)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Reguła cykliczności nie zawiera żadnych dni tygodnia.");

        // Pobierz istniejące StartTime z tej serii (wykrywanie duplikatów)
        var existingEvents = await _events.GetBySeriesAsync(seriesId, ct);
        var existingStartTimes = existingEvents.Select(e => e.StartTime).ToHashSet();

        // Resolve overrides vs. series defaults
        var locationId = request.OverrideLocationId ?? series.DefaultLocationId;
        var groupId    = request.OverrideGroupId    ?? series.DefaultGroupId;
        var color      = request.OverrideColor      ?? series.DefaultColor;

        // Generuj zajęcia dla każdego pasującego dnia w zakresie
        const int maxEvents = 500;
        var toCreate     = new List<Event>();
        var skippedCount = 0;
        var currentDate  = request.DateFrom;

        while (currentDate <= request.DateTo && toCreate.Count < maxEvents)
        {
            if (daysOfWeek.Contains(currentDate.DayOfWeek))
            {
                var startUtc = new DateTime(
                    currentDate.Year, currentDate.Month, currentDate.Day,
                    request.StartTime.Hour, request.StartTime.Minute, 0,
                    DateTimeKind.Utc);

                var endUtc = new DateTime(
                    currentDate.Year, currentDate.Month, currentDate.Day,
                    request.EndTime.Hour, request.EndTime.Minute, 0,
                    DateTimeKind.Utc);

                if (existingStartTimes.Contains(startUtc))
                {
                    skippedCount++;
                }
                else
                {
                    toCreate.Add(new Event
                    {
                        OrganizationId = organizationId,
                        EventSeriesId  = seriesId,
                        Title          = series.Title,
                        StartTime      = startUtc,
                        EndTime        = endUtc,
                        LocationId     = locationId,
                        GroupId        = groupId,
                        EventType      = series.DefaultEventType,
                        Color          = color,
                        Status         = EventStatus.Scheduled,
                    });
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        await _events.AddRangeAsync(toCreate, ct);

        return new GenerateEventsResponse
        {
            GeneratedCount = toCreate.Count,
            SkippedCount   = skippedCount,
        };
    }

    /// <summary>
    /// Parsuje RRULE (format "WEEKLY;BYDAY=MO,TU" lub "FREQ=WEEKLY;BYDAY=MO,TU")
    /// i zwraca zestaw dni tygodnia.
    /// </summary>
    private static HashSet<DayOfWeek> ParseRruleDays(string rule)
    {
        var result = new HashSet<DayOfWeek>();
        var match  = Regex.Match(rule, @"BYDAY=([A-Z,]+)", RegexOptions.IgnoreCase);
        if (!match.Success) return result;

        foreach (var code in match.Groups[1].Value.Split(','))
        {
            if (DayCodeMap.TryGetValue(code.Trim().ToUpperInvariant(), out var dow))
                result.Add(dow);
        }

        return result;
    }
}
