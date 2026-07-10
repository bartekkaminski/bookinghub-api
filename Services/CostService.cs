using BookingHub.Api.Dtos.Cost;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania stawkami i kalkulacji kosztów.
/// </summary>
public sealed class CostService : ICostService
{
    private readonly IGroupCostRateRepository _groupRates;
    private readonly ITrainerSessionRateRepository _trainerRates;
    private readonly IGroupRepository _groups;
    private readonly IOrganizationMemberRepository _members;
    private readonly IEventEnrollmentRepository _enrollments;
    private readonly IEventRepository _events;
    private readonly ILogger<CostService> _logger;

    public CostService(
        IGroupCostRateRepository groupRates,
        ITrainerSessionRateRepository trainerRates,
        IGroupRepository groups,
        IOrganizationMemberRepository members,
        IEventEnrollmentRepository enrollments,
        IEventRepository events,
        ILogger<CostService> logger)
    {
        _groupRates   = groupRates;
        _trainerRates = trainerRates;
        _groups       = groups;
        _members      = members;
        _enrollments  = enrollments;
        _events       = events;
        _logger       = logger;
    }

    // ── Group Rates ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GroupCostRateResponse>> GetGroupRatesAsync(Guid groupId, CancellationToken ct = default)
    {
        var exists = await _groups.ExistsAsync(groupId, ct);
        if (!exists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Grupa {groupId} nie istnieje.");

        var rates = await _groupRates.GetByGroupAsync(groupId, ct);
        return rates.Select(r => r.ToResponse()).ToList();
    }

    /// <inheritdoc/>
    public async Task<GroupCostRateResponse?> GetCurrentGroupRateAsync(Guid groupId, CancellationToken ct = default)
    {
        var rate = await _groupRates.GetCurrentByGroupAsync(groupId, ct);
        return rate?.ToResponse();
    }

    /// <inheritdoc/>
    public async Task<GroupCostRateResponse> AddGroupRateAsync(Guid groupId, AddGroupCostRateRequest request, CancellationToken ct = default)
    {
        var exists = await _groups.ExistsAsync(groupId, ct);
        if (!exists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Grupa {groupId} nie istnieje.");

        var active = await _groupRates.GetCurrentByGroupAsync(groupId, ct);
        if (active is not null)
        {
            var autoClose = request.ValidFrom.AddDays(-1);
            if (autoClose < active.ValidFrom)
                throw new ServiceException(ServiceErrorCode.InvalidRateDateRange,
                    "Data ValidFrom nowej stawki musi być późniejsza niż ValidFrom poprzedniej.");

            active.ValidTo = autoClose;
            await _groupRates.UpdateAsync(active, ct);
        }

        var entity  = request.ToEntity(groupId);
        var created = await _groupRates.AddAsync(entity, ct);
        return created.ToResponse();
    }

    /// <inheritdoc/>
    public async Task<GroupCostRateResponse> CloseGroupRateAsync(Guid rateId, CloseGroupCostRateRequest request, CancellationToken ct = default)
    {
        var rate = await _groupRates.GetByIdAsync(rateId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Stawka {rateId} nie istnieje.");

        if (rate.ValidTo is not null)
            throw new ServiceException(ServiceErrorCode.Conflict, "Stawka jest już zamknięta.");

        if (request.ValidTo < rate.ValidFrom)
            throw new ServiceException(ServiceErrorCode.InvalidRateDateRange,
                "ValidTo musi być >= ValidFrom.", nameof(request.ValidTo));

        rate.ValidTo = request.ValidTo;
        await _groupRates.UpdateAsync(rate, ct);
        return rate.ToResponse();
    }

    /// <inheritdoc/>
    public async Task DeleteGroupRateAsync(Guid rateId, CancellationToken ct = default)
    {
        var rate = await _groupRates.GetByIdAsync(rateId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Stawka {rateId} nie istnieje.");

        if (rate.ValidTo is not null)
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Nie można usunąć zamkniętej stawki.");

        await _groupRates.DeleteAsync(rateId, ct);
    }

    // ── Trainer Rates ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TrainerSessionRateResponse>> GetTrainerRatesAsync(Guid trainerMemberId, CancellationToken ct = default)
    {
        var rates = await _trainerRates.GetByTrainerAsync(trainerMemberId, ct);
        return rates.Select(r => r.ToResponse()).ToList();
    }

    /// <inheritdoc/>
    public async Task<TrainerSessionRateResponse?> GetCurrentTrainerRateAsync(Guid trainerMemberId, CancellationToken ct = default)
    {
        var rate = await _trainerRates.GetCurrentByTrainerAsync(trainerMemberId, ct);
        return rate?.ToResponse();
    }

    /// <inheritdoc/>
    public async Task<TrainerSessionRateResponse> AddTrainerRateAsync(Guid trainerMemberId, AddTrainerSessionRateRequest request, CancellationToken ct = default)
    {
        var memberExists = await _members.ExistsAsync(trainerMemberId, ct);
        if (!memberExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Trener {trainerMemberId} nie istnieje.");

        var active = await _trainerRates.GetCurrentByTrainerAsync(trainerMemberId, ct);
        if (active is not null)
        {
            var autoClose = request.ValidFrom.AddDays(-1);
            if (autoClose < active.ValidFrom)
                throw new ServiceException(ServiceErrorCode.InvalidRateDateRange,
                    "Data ValidFrom nowej stawki musi być późniejsza niż ValidFrom poprzedniej.");

            active.ValidTo = autoClose;
            await _trainerRates.UpdateAsync(active, ct);
        }

        var entity  = request.ToEntity(trainerMemberId);
        var created = await _trainerRates.AddAsync(entity, ct);
        return created.ToResponse();
    }

    /// <inheritdoc/>
    public async Task<TrainerSessionRateResponse> CloseTrainerRateAsync(Guid rateId, CloseTrainerSessionRateRequest request, CancellationToken ct = default)
    {
        var rate = await _trainerRates.GetByIdAsync(rateId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Stawka {rateId} nie istnieje.");

        if (rate.ValidTo is not null)
            throw new ServiceException(ServiceErrorCode.Conflict, "Stawka jest już zamknięta.");

        if (request.ValidTo < rate.ValidFrom)
            throw new ServiceException(ServiceErrorCode.InvalidRateDateRange,
                "ValidTo musi być >= ValidFrom.", nameof(request.ValidTo));

        rate.ValidTo = request.ValidTo;
        await _trainerRates.UpdateAsync(rate, ct);
        return rate.ToResponse();
    }

    /// <inheritdoc/>
    public async Task DeleteTrainerRateAsync(Guid rateId, CancellationToken ct = default)
    {
        var rate = await _trainerRates.GetByIdAsync(rateId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Stawka {rateId} nie istnieje.");

        if (rate.ValidTo is not null)
            throw new ServiceException(ServiceErrorCode.Conflict, "Nie można usunąć zamkniętej stawki.");

        await _trainerRates.DeleteAsync(rateId, ct);
    }

    // ── Billing ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<MemberMonthlyBillResponse> CalculateMemberMonthlyBillAsync(Guid memberId, int year, int month, CancellationToken ct = default)
    {
        var member = await _members.GetWithDetailsAsync(memberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Uczestnik {memberId} nie istnieje.");

        var monthStart = new DateOnly(year, month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);
        var dtStart    = monthStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dtEnd      = monthEnd.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var groupFees          = new List<GroupFeeItem>();
        var individualFees     = new List<IndividualSessionFeeItem>();
        var oneTimeFees        = new List<OneTimeFeeItem>();

        // 1. Składki za grupy (GroupTraining)
        foreach (var groupMembership in member.GroupMemberships)
        {
            var rate = await _groupRates.GetRateOnDateAsync(groupMembership.GroupId, monthStart, ct);
            if (rate is not null)
            {
                groupFees.Add(new GroupFeeItem
                {
                    GroupId     = groupMembership.GroupId,
                    GroupName   = groupMembership.Group?.Name ?? string.Empty,
                    MonthlyCost = rate.MonthlyCost,
                    Currency    = rate.Currency,
                });
            }
        }

        // 2. Uczestniczone zajęcia w danym miesiącu
        var attendedEnrollments = await _enrollments.GetAttendedByMemberInPeriodAsync(memberId, dtStart, dtEnd, ct);

        foreach (var enrollment in attendedEnrollments)
        {
            if (enrollment.Event is null) continue;

            var ev = enrollment.Event;

            if (ev.EventType == EventType.IndividualSession)
            {
                // Koszt = stawka trenera × czas [h] ÷ liczba uczestników
                var trainerIds = ev.Trainers.Select(t => t.OrganizationMemberId).ToList();
                if (trainerIds.Count == 0) continue;

                var primaryTrainerId = trainerIds.First();
                var trainerRate = await _trainerRates.GetRateOnDateAsync(
                    primaryTrainerId, DateOnly.FromDateTime(ev.StartTime), ct);

                if (trainerRate is null) continue;

                var durationHours = (decimal)(ev.EndTime - ev.StartTime).TotalHours;
                var participants  = Math.Max(1, ev.Enrollments.Count(e => e.Status == EventEnrollmentStatus.Attended));
                var cost          = Math.Round(trainerRate.RatePerHour * durationHours / participants, 2);

                individualFees.Add(new IndividualSessionFeeItem
                {
                    EventId            = ev.Id,
                    EventTitle         = ev.Title,
                    EventStartTime     = ev.StartTime,
                    DurationHours      = durationHours,
                    TrainerRatePerHour = trainerRate.RatePerHour,
                    ParticipantsCount  = participants,
                    Cost               = cost,
                    Currency           = trainerRate.Currency,
                });
            }
            else if (ev.EventType == EventType.Camp && ev.UnitCost.HasValue)
            {
                oneTimeFees.Add(new OneTimeFeeItem
                {
                    EventId        = ev.Id,
                    EventTitle     = ev.Title,
                    EventStartTime = ev.StartTime,
                    UnitCost       = ev.UnitCost.Value,
                    Currency       = ev.Currency ?? "PLN",
                });
            }
        }

        var totalGroupFees      = groupFees.Sum(f => f.MonthlyCost);
        var totalIndividualFees = individualFees.Sum(f => f.Cost);
        var totalOneTimeFees    = oneTimeFees.Sum(f => f.UnitCost);

        return new MemberMonthlyBillResponse
        {
            OrganizationMemberId = memberId,
            MemberDisplayName    = member.ResolveDisplayName(),
            Year                 = year,
            Month                = month,
            GroupFees            = groupFees,
            IndividualSessionFees= individualFees,
            OneTimeFees          = oneTimeFees,
            TotalGroupFees       = totalGroupFees,
            TotalIndividualFees  = totalIndividualFees,
            TotalOneTimeFees     = totalOneTimeFees,
            GrandTotal           = totalGroupFees + totalIndividualFees + totalOneTimeFees,
            Currency             = "PLN",
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MemberMonthlyBillResponse>> CalculateOrganizationMonthlyBillAsync(Guid organizationId, int year, int month, CancellationToken ct = default)
    {
        var participants = await _members.GetByRoleAsync(organizationId, MemberRole.Participant, ct);
        var activeParts  = participants.Where(p => p.IsActive).ToList();

        var results = new List<MemberMonthlyBillResponse>(activeParts.Count);
        foreach (var participant in activeParts)
        {
            try
            {
                var bill = await CalculateMemberMonthlyBillAsync(participant.Id, year, month, ct);
                results.Add(bill);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Błąd kalkulacji rachunku dla uczestnika {MemberId} za {Year}/{Month}.",
                    participant.Id, year, month);
            }
        }

        return results;
    }
}
