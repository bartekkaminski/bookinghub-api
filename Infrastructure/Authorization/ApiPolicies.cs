namespace BookingHub.Api.Infrastructure.Authorization;

/// <summary>
/// Nazwy polityk autoryzacji ASP.NET Core.
/// Używane z [Authorize(Policy = ApiPolicies.AuthenticatedUser)] w kontrolerach.
///
/// UWAGA: Autoryzacja per-rola-w-organizacji NIE korzysta z tych polityk —
/// używa filtru [RequireOrgMembership(...)], który ma dostęp do parametrów trasy.
/// </summary>
public static class ApiPolicies
{
    /// <summary>Wymagany ważny token JWT — dowolny zalogowany użytkownik.</summary>
    public const string AuthenticatedUser = "AuthenticatedUser";
}
