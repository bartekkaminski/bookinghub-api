namespace BookingHub.Api.Models;

/// <summary>
/// Przypisanie rangi do członka w ramach konkretnej dyscypliny.
/// Członek może mieć wiele wpisów (jeden per dyscyplina), ale co najwyżej jeden per dyscyplina
/// — wymuszone unikalnym indeksem (MemberId, DisciplineId) w <see cref="Data.AppDbContext"/>.
/// </summary>
public class MemberRank : BaseEntity
{
    public Guid MemberId { get; set; }
    public Guid DisciplineId { get; set; }
    public Guid RankId { get; set; }

    public OrganizationMember Member { get; set; } = null!;
    public Discipline Discipline { get; set; } = null!;
    public OrganizationRank Rank { get; set; } = null!;
}
