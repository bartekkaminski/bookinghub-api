using BookingHub.Api.Dtos.Availability;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania dostępnością uczestników i trenerów.
/// </summary>
public sealed class AvailabilityService : IAvailabilityService
{
    private readonly IMemberAvailabilityRepository _availability;
    private readonly IOrganizationMemberRepository _members;
    private readonly IEventRepository              _events;

    public AvailabilityService(
        IMemberAvailabilityRepository availability,
        IOrganizationMemberRepository members,
        IEventRepository events)
    {
        _availability = availability;
        _members      = members;
        _events       = events;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AvailabilitySlotResponse>> GetByMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        var slots = await _availability.GetByMemberAsync(memberId, ct);
        return slots.Select(s => s.ToResponse()).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AvailabilitySlotResponse>> GetByMemberAndDateAsync(Guid memberId, DateOnly date, CancellationToken ct = default)
    {
        var slots = await _availability.GetByMemberOnDateAsync(memberId, date, ct);
        return slots.Select(s => s.ToResponse()).ToList();
    }

    /// <inheritdoc/>
    public async Task<AvailabilitySlotResponse> AddSlotAsync(Guid memberId, AddAvailabilitySlotRequest request, CancellationToken ct = default)
    {
        var memberExists = await _members.ExistsAsync(memberId, ct);
        if (!memberExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Członek {memberId} nie istnieje.");

        ValidateSlot(request.TimeFrom, request.TimeTo, request.ValidFrom, request.ValidTo);

        var entity  = request.ToEntity(memberId);
        var created = await _availability.AddAsync(entity, ct);
        return created.ToResponse();
    }

    /// <inheritdoc/>
    public async Task<AvailabilitySlotResponse> UpdateSlotAsync(Guid slotId, UpdateAvailabilitySlotRequest request, CancellationToken ct = default)
    {
        var slot = await _availability.GetByIdAsync(slotId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Slot {slotId} nie istnieje.");

        ValidateSlot(request.TimeFrom, request.TimeTo, request.ValidFrom, request.ValidTo);

        slot.ApplyUpdate(request);
        await _availability.UpdateAsync(slot, ct);
        return slot.ToResponse();
    }

    /// <inheritdoc/>
    public async Task DeleteSlotAsync(Guid slotId, CancellationToken ct = default)
    {
        var exists = await _availability.ExistsAsync(slotId, ct);
        if (!exists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Slot {slotId} nie istnieje.");

        await _availability.DeleteAsync(slotId, ct);
    }

    /// <inheritdoc/>
    public async Task<AvailabilityCheckResponse> CheckAvailabilityAsync(IReadOnlyList<Guid> memberIds, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (from >= to)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Data 'from' musi być wcześniejsza niż 'to'.");

        var checkDay  = DateOnly.FromDateTime(from);
        var timeFrom  = TimeOnly.FromDateTime(from);
        var timeTo    = TimeOnly.FromDateTime(to);
        var dayOfWeek = from.DayOfWeek;

        var results = new List<MemberAvailabilityInfo>();

        foreach (var memberId in memberIds)
        {
            var slots = await _availability.GetByMemberOnDateAsync(memberId, checkDay, ct);
            var matching = slots
                .Where(s => s.DayOfWeek == dayOfWeek
                    && s.TimeFrom <= timeFrom
                    && s.TimeTo >= timeTo)
                .Select(s => s.ToResponse())
                .ToList();

            var member = await _members.GetByIdAsync(memberId, ct);

            results.Add(new MemberAvailabilityInfo
            {
                MemberId      = memberId,
                DisplayName   = member?.ResolveDisplayName() ?? string.Empty,
                IsAvailable   = matching.Any(),
                MatchingSlots = matching,
            });
        }

        return new AvailabilityCheckResponse
        {
            CheckFrom = from,
            CheckTo   = to,
            Members   = results,
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MemberScheduleResponse>> GetMemberScheduleAsync(
        Guid memberId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt   = to.ToDateTime(TimeOnly.MaxValue,  DateTimeKind.Utc);

        // ── 1. Wszystkie sloty jednorazowo — brak N+1 ─────────────────────────
        // _availability.GetByMemberAsync zwraca IReadOnlyList<MemberAvailability> (encje)
        var allSlots = await _availability.GetByMemberAsync(memberId, ct);

        // ── 2. Zajęcia obu ról, unia przez Id, bez Cancelled ──────────────────
        // GetCalendarForMemberAsync: individual + team enrollments (już z filtrem != Cancelled)
        // GetByTrainerAsync: zajęcia gdzie członek jest trenerem
        var participantEvents = await _events.GetCalendarForMemberAsync(memberId, fromDt, toDt, ct);
        var trainerEvents     = await _events.GetByTrainerAsync(memberId, fromDt, toDt, ct);

        var allEvents = participantEvents
            .Union(trainerEvents, EventIdComparer.Instance)
            .Where(e => e.Status != EventStatus.Cancelled)
            .ToList();

        // ── 3. Per dzień: wolne sloty + WSZYSTKIE zajęcia (także poza slotami) ─
        var result = new List<MemberScheduleResponse>();

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var activeSlots = allSlots
                .Where(s =>
                    s.DayOfWeek == day.DayOfWeek &&
                    (s.ValidFrom == null || s.ValidFrom <= day) &&
                    (s.ValidTo   == null || s.ValidTo   >= day))
                .OrderBy(s => s.TimeFrom)
                .ToList();

            var dayStartUtc = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var dayEndUtc   = day.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            // Zajęcia nachodzące na ten dzień (zapis uczestnika lub przypisanie trenera)
            var dayBusy = allEvents
                .Where(e => e.StartTime < dayEndUtc && e.EndTime > dayStartUtc)
                .Select(e =>
                {
                    var bFrom = TimeOnly.FromTimeSpan(e.StartTime.TimeOfDay);
                    var bTo   = TimeOnly.FromTimeSpan(e.EndTime.TimeOfDay);

                    if (e.StartTime.Date < dayStartUtc.Date) bFrom = TimeOnly.MinValue;
                    if (e.EndTime.Date > dayStartUtc.Date)   bTo   = TimeOnly.MaxValue;

                    return new BusyInterval(bFrom, bTo, e);
                })
                .OrderBy(b => b.From)
                .ToList();

            if (activeSlots.Count == 0 && dayBusy.Count == 0)
                continue;

            var blocks = new List<ScheduleBlock>();

            // Wolne fragmenty slotów dostępności (zajęcia wycinają Available)
            foreach (var slot in activeSlots)
            {
                var overlapping = dayBusy
                    .Where(b => b.From < slot.TimeTo && b.To > slot.TimeFrom)
                    .ToList();

                blocks.AddRange(MergeSlotWithBusy(slot.Id, slot.TimeFrom, slot.TimeTo, overlapping));
            }

            // Zajęcia ZAWSZE jako Busy — także gdy nie ma slotu dostępności tego dnia
            // (SlotId = Empty → frontend nie otwiera edycji slotu)
            foreach (var b in dayBusy)
            {
                if (b.To <= b.From) continue;
                blocks.Add(new ScheduleBlock
                {
                    TimeFrom = b.From,
                    TimeTo   = b.To,
                    Type     = ScheduleBlockType.Busy,
                    SlotId   = Guid.Empty,
                    Event    = new ScheduleEventInfo
                    {
                        EventId   = b.Event.Id,
                        Title     = b.Event.Title,
                        Color     = b.Event.Color ?? b.Event.Group?.Color,
                        EventType = b.Event.EventType.ToString(),
                    },
                });
            }

            // Usuń Busy wygenerowane wewnątrz MergeSlotWithBusy (zostają Available + pełne Busy z eventów)
            blocks = blocks
                .Where(bl => bl.Type == ScheduleBlockType.Available || bl.SlotId == Guid.Empty)
                .OrderBy(bl => bl.TimeFrom)
                .ThenBy(bl => bl.Type)
                .ToList();

            if (blocks.Count > 0)
                result.Add(new MemberScheduleResponse { Date = day, Blocks = blocks });
        }

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Scala slot dostępności z posortowaną listą zajętych interwałów.
    /// Kursor przesuwa się do max(cursor, bTo), obsługując nakładające się zajęcia.
    /// </summary>
    private static IEnumerable<ScheduleBlock> MergeSlotWithBusy(
        Guid slotId, TimeOnly slotFrom, TimeOnly slotTo,
        IReadOnlyList<BusyInterval> busy)
    {
        var cursor = slotFrom;

        foreach (var b in busy)
        {
            var bFrom = b.From < slotFrom ? slotFrom : b.From;
            var bTo   = b.To   > slotTo   ? slotTo   : b.To;

            if (bTo <= cursor) continue;

            if (bFrom > cursor)
            {
                yield return new ScheduleBlock
                {
                    TimeFrom = cursor,
                    TimeTo   = bFrom,
                    Type     = ScheduleBlockType.Available,
                    SlotId   = slotId,
                };
            }

            var busyStart = bFrom < cursor ? cursor : bFrom;
            if (busyStart < bTo)
            {
                yield return new ScheduleBlock
                {
                    TimeFrom = busyStart,
                    TimeTo   = bTo,
                    Type     = ScheduleBlockType.Busy,
                    SlotId   = slotId,
                    Event    = new ScheduleEventInfo
                    {
                        EventId   = b.Event.Id,
                        Title     = b.Event.Title,
                        Color     = b.Event.Color ?? b.Event.Group?.Color,
                        EventType = b.Event.EventType.ToString(),
                    },
                };
            }

            if (bTo > cursor) cursor = bTo;
        }

        if (cursor < slotTo)
            yield return new ScheduleBlock
            {
                TimeFrom = cursor,
                TimeTo   = slotTo,
                Type     = ScheduleBlockType.Available,
                SlotId   = slotId,
            };
    }

    private record BusyInterval(TimeOnly From, TimeOnly To, Event Event);

    /// <summary>
    /// Comparer eventów po Id — deduplikacja unii GetCalendarForMemberAsync
    /// i GetByTrainerAsync (gdy członek jest jednocześnie trenerem i uczestnikiem eventu).
    /// </summary>
    private sealed class EventIdComparer : IEqualityComparer<Event>
    {
        public static readonly EventIdComparer Instance = new();
        public bool Equals(Event? x, Event? y) => x?.Id == y?.Id;
        public int GetHashCode(Event obj)       => obj.Id.GetHashCode();
    }

    private static void ValidateSlot(TimeOnly timeFrom, TimeOnly timeTo, DateOnly? validFrom, DateOnly? validTo)
    {
        if (timeFrom >= timeTo)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "TimeFrom musi być wcześniejsze niż TimeTo.", nameof(timeFrom));

        if (validFrom.HasValue && validTo.HasValue && validFrom.Value > validTo.Value)
            throw new ServiceException(ServiceErrorCode.InvalidRateDateRange,
                "ValidFrom nie może być późniejsze niż ValidTo.");
    }
}
