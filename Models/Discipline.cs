namespace BookingHub.Api.Models;

/// <summary>
/// Dyscyplina w organizacji — grupuje rangi w niezależny "tor" awansu,
/// np. "Latino", "Standard" w szkole tańca albo "Pasy" w klubie karate.
/// Członek może mieć co najwyżej jedną rangę per dyscyplina (patrz <see cref="MemberRank"/>).
/// </summary>
public class Discipline : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Nazwa dyscypliny, np. "Latino", "Standard".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Opcjonalny kolor HEX przypisany do dyscypliny, np. "#F59E0B".
    /// Null = brak koloru (wyświetlany szary).
    /// </summary>
    public string? Color { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<OrganizationRank> Ranks { get; set; } = [];
}
