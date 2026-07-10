namespace BookingHub.Api.Infrastructure.Authorization;

/// <summary>
/// Nazwy ról używane w politykach autoryzacji per-organizacja.
/// Stałe muszą być identyczne z wartościami MemberRole enum (string storage).
/// </summary>
public static class OrgRoles
{
    public const string Admin       = "Admin";
    public const string Manager     = "Manager";
    public const string Trainer     = "Trainer";
    public const string Participant = "Participant";
}
