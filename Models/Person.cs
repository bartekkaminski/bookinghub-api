namespace BookingHub.Api.Models;

public class Person : BaseEntity
{
    /// <summary>
    /// Powiązanie z kontem logowania. Null = uczestnik bez konta (np. dziecko bez e-maila).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>Nullable — admin uzupełnia po utworzeniu, Kinde może nie zwrócić imienia</summary>
    public string? FirstName { get; set; }

    /// <summary>Nullable — admin uzupełnia po utworzeniu</summary>
    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Globalne zdjęcie profilowe — może być nadpisane per-organizacja w OrganizationMember</summary>
    public string? PhotoUrl { get; set; }

    public User? User { get; set; }

    public ICollection<OrganizationMember> Memberships { get; set; } = [];

    /// <summary>Relacje gdzie ta osoba jest rodzicem/opiekunem</summary>
    public ICollection<ParentChildRelation> ChildRelations { get; set; } = [];

    /// <summary>Relacje gdzie ta osoba jest dzieckiem</summary>
    public ICollection<ParentChildRelation> ParentRelations { get; set; } = [];
}
