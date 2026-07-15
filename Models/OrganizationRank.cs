namespace BookingHub.Api.Models;

/// <summary>
/// Ranga w organizacji — etykieta przypisywana do członków przez administratora.
/// Każdy członek może mieć co najwyżej jedną rangę w danej organizacji.
/// </summary>
public class OrganizationRank : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Nazwa rangi, np. "Złota", "Srebrna", "Kapitan".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Opcjonalny kolor HEX przypisany do rangi, np. "#F59E0B".
    /// Null = brak koloru (wyświetlany szary).
    /// </summary>
    public string? Color { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<OrganizationMember> Members { get; set; } = [];
}
