using System.ComponentModel.DataAnnotations;

namespace BookingHub.Api.Dtos.Team;

/// <summary>Skrócone dane zespołu — do list.</summary>
public sealed class TeamSummaryResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
    public int? Priority { get; set; }
    public int MembersCount { get; set; }
    public IReadOnlyList<string> MemberNames { get; set; } = [];
}

/// <summary>Pełne dane zespołu — widok szczegółowy.</summary>
public sealed class TeamDetailResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
    public int? Priority { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<TeamMemberInfo> Members { get; set; } = [];
    public IReadOnlyList<TeamGroupInfo> Groups { get; set; } = [];
    public IReadOnlyList<TeamTrainerInfo> Trainers { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class TeamMemberInfo
{
    public Guid MemberId { get; set; }
    public Guid PersonId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Color { get; set; }
}

public sealed class TeamGroupInfo
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public sealed class TeamTrainerInfo
{
    public Guid TrainerMemberId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Color { get; set; }
}

/// <summary>Dane do tworzenia zespołu.</summary>
public sealed class CreateTeamRequest
{
    [StringLength(200)]
    public string? Name { get; set; }

    [Range(1, int.MaxValue)]
    public int? Priority { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>Dane do aktualizacji zespołu.</summary>
public sealed class UpdateTeamRequest
{
    [StringLength(200)]
    public string? Name { get; set; }

    [Range(1, int.MaxValue)]
    public int? Priority { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Żądanie dodania uczestnika do zespołu.</summary>
public sealed class AddMemberToTeamRequest
{
    [Required]
    public Guid OrganizationMemberId { get; set; }
}

/// <summary>Żądanie przypisania stałego trenera do zespołu.</summary>
public sealed class AssignTrainerToTeamRequest
{
    [Required]
    public Guid TrainerMemberId { get; set; }
}
