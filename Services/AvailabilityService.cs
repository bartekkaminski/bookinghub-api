using BookingHub.Api.Dtos.Availability;
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

    public AvailabilityService(
        IMemberAvailabilityRepository availability,
        IOrganizationMemberRepository members)
    {
        _availability = availability;
        _members      = members;
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

    private static void ValidateSlot(TimeOnly timeFrom, TimeOnly timeTo, DateOnly? validFrom, DateOnly? validTo)
    {
        if (timeFrom >= timeTo)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "TimeFrom musi być wcześniejsze niż TimeTo.", nameof(timeFrom));

        if (validFrom.HasValue && validTo.HasValue && validFrom.Value >= validTo.Value)
            throw new ServiceException(ServiceErrorCode.InvalidRateDateRange,
                "ValidFrom musi być wcześniejsze niż ValidTo.");
    }
}
