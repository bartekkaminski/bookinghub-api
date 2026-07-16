using System.ComponentModel.DataAnnotations;

namespace BookingHub.Api.Dtos.Group;

/// <summary>Skrócone dane grupy zajęciowej — do list.</summary>
public sealed class GroupSummaryResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; }
    public int MembersCount { get; set; }
    public int TeamsCount { get; set; }
    /// <summary>Aktualna stawka miesięczna, null jeśli nie zdefiniowana.</summary>
    public decimal? CurrentMonthlyCost { get; set; }
    public string? CurrentCostCurrency { get; set; }
}

/// <summary>Pełne dane grupy — widok szczegółowy.</summary>
public sealed class GroupDetailResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<GroupMemberInfo> Members { get; set; } = [];
    public IReadOnlyList<GroupTeamInfo> Teams { get; set; } = [];
    /// <summary>
    /// Deduplikowana lista wszystkich unikalnych osób w grupie — uczestnicy bezpośredni
    /// oraz osoby wchodzące w skład przypisanych zespołów, scaleni po OrganizationMemberId.
    /// </summary>
    public IReadOnlyList<GroupEffectiveMemberInfo> EffectiveMembers { get; set; } = [];
    public IReadOnlyList<GroupTrainerInfo> Trainers { get; set; } = [];
    public IReadOnlyList<GroupCostRateInfo> CostRates { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class GroupMemberInfo
{
    public Guid MemberId { get; set; }
    public Guid PersonId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Color { get; set; }
    public int? Priority { get; set; }
    public DateTime JoinedAt { get; set; }
}

/// <summary>Unikalna osoba w grupie wraz z informacją, z jakiego źródła (uczestnik/zespoły) pochodzi.</summary>
public sealed class GroupEffectiveMemberInfo
{
    public Guid MemberId { get; set; }
    public Guid PersonId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Color { get; set; }
    /// <summary>True, jeśli osoba jest dodana do grupy bezpośrednio (GroupMember).</summary>
    public bool IsDirectParticipant { get; set; }
    /// <summary>Nazwy zespołów przypisanych do grupy, w których ta osoba jest członkiem.</summary>
    public IReadOnlyList<string> TeamNames { get; set; } = [];
}

public sealed class GroupTeamInfo
{
    public Guid TeamId { get; set; }
    public string? TeamName { get; set; }
    public int? Priority { get; set; }
    public int MembersCount { get; set; }
}

public sealed class GroupTrainerInfo
{
    public Guid TrainerMemberId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public sealed class GroupCostRateInfo
{
    public Guid Id { get; set; }
    public decimal MonthlyCost { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsCurrent => ValidTo is null;
}

/// <summary>Dane do tworzenia grupy zajęciowej.</summary>
public sealed class CreateGroupRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Kolor musi być w formacie hex #RRGGBB.")]
    public string? Color { get; set; }
}

/// <summary>Dane do aktualizacji grupy zajęciowej.</summary>
public sealed class UpdateGroupRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Kolor musi być w formacie hex #RRGGBB.")]
    public string? Color { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Żądanie dodania uczestnika do grupy.</summary>
public sealed class AddMemberToGroupRequest
{
    [Required]
    public Guid OrganizationMemberId { get; set; }
}

/// <summary>Żądanie przypisania zespołu do grupy.</summary>
public sealed class AddTeamToGroupRequest
{
    [Required]
    public Guid TeamId { get; set; }
}

/// <summary>Żądanie przypisania stałego trenera do grupy.</summary>
public sealed class AssignTrainerToGroupRequest
{
    [Required]
    public Guid TrainerMemberId { get; set; }
}
