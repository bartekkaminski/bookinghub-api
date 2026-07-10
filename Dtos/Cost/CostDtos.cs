using System.ComponentModel.DataAnnotations;

namespace BookingHub.Api.Dtos.Cost;

// ─── Group Cost Rates ────────────────────────────────────────────────────────

/// <summary>Dane stawki miesięcznej grupy zajęciowej.</summary>
public sealed class GroupCostRateResponse
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public decimal MonthlyCost { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateOnly ValidFrom { get; set; }
    /// <summary>Null = aktualnie obowiązująca stawka.</summary>
    public DateOnly? ValidTo { get; set; }
    public bool IsCurrent => ValidTo is null;
}

/// <summary>Dane do dodania nowej stawki miesięcznej dla grupy.</summary>
public sealed class AddGroupCostRateRequest
{
    [Required]
    [Range(0.01, 999999.99)]
    public decimal MonthlyCost { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "PLN";

    [Required]
    public DateOnly ValidFrom { get; set; }
}

/// <summary>Zamknięcie aktualnej stawki (ustawienie ValidTo).</summary>
public sealed class CloseGroupCostRateRequest
{
    [Required]
    public DateOnly ValidTo { get; set; }
}

// ─── Trainer Session Rates ──────────────────────────────────────────────────

/// <summary>Dane stawki godzinowej trenera.</summary>
public sealed class TrainerSessionRateResponse
{
    public Guid Id { get; set; }
    public Guid TrainerMemberId { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public decimal RatePerHour { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateOnly ValidFrom { get; set; }
    /// <summary>Null = aktualnie obowiązująca stawka.</summary>
    public DateOnly? ValidTo { get; set; }
    public bool IsCurrent => ValidTo is null;
}

/// <summary>Dane do dodania nowej stawki godzinowej trenera.</summary>
public sealed class AddTrainerSessionRateRequest
{
    [Required]
    [Range(0.01, 9999.99)]
    public decimal RatePerHour { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "PLN";

    [Required]
    public DateOnly ValidFrom { get; set; }
}

/// <summary>Zamknięcie aktualnej stawki trenera.</summary>
public sealed class CloseTrainerSessionRateRequest
{
    [Required]
    public DateOnly ValidTo { get; set; }
}

// ─── Billing ─────────────────────────────────────────────────────────────────

/// <summary>Miesięczny rachunek uczestnika — suma wszystkich składników kosztów.</summary>
public sealed class MemberMonthlyBillResponse
{
    public Guid OrganizationMemberId { get; set; }
    public string MemberDisplayName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>Składki za uczestnictwo w grupach.</summary>
    public IReadOnlyList<GroupFeeItem> GroupFees { get; set; } = [];

    /// <summary>Koszty zajęć indywidualnych (stawka trenera × czas ÷ liczba uczestników).</summary>
    public IReadOnlyList<IndividualSessionFeeItem> IndividualSessionFees { get; set; } = [];

    /// <summary>Jednorazowe opłaty za obozy / eventy z UnitCost.</summary>
    public IReadOnlyList<OneTimeFeeItem> OneTimeFees { get; set; } = [];

    public decimal TotalGroupFees { get; set; }
    public decimal TotalIndividualFees { get; set; }
    public decimal TotalOneTimeFees { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "PLN";
}

public sealed class GroupFeeItem
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public decimal MonthlyCost { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public sealed class IndividualSessionFeeItem
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }
    public decimal DurationHours { get; set; }
    public decimal TrainerRatePerHour { get; set; }
    public int ParticipantsCount { get; set; }
    public decimal Cost { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public sealed class OneTimeFeeItem
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }
    public decimal UnitCost { get; set; }
    public string Currency { get; set; } = string.Empty;
}
