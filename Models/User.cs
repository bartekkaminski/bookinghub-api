namespace BookingHub.Api.Models;

public class User : BaseEntity
{
    /// <summary>Identyfikator użytkownika u zewnętrznego dostawcy auth (claim: sub)</summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>Nazwa dostawcy auth: "kinde", "auth0", "clerk" itp.</summary>
    public string AuthProvider { get; set; } = string.Empty;

    /// <summary>Adres e-mail użytkownika</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Czy konto jest aktywne globalnie (we wszystkich organizacjach).
    /// False blokuje dostęp na poziomie API niezależnie od OrganizationMember.IsActive.
    /// Synchronizowane z Kinde (is_suspended) przy zmianie.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Preferowany język UI: "pl" lub "en". Domyślnie "pl".</summary>
    public string PreferredLanguage { get; set; } = "pl";

    /// <summary>
    /// Unikalny kod profilu (8 znaków, A-Z 2-9) wyświetlany użytkownikowi.
    /// Admin podaje ten kod przy dodawaniu istniejącej osoby do organizacji
    /// — zamiast emaila, aby zapobiec enumeracji kont.
    /// </summary>
    public string ProfileCode { get; set; } = string.Empty;

    public Person? Person { get; set; }
    public ICollection<UserDeviceToken> DeviceTokens { get; set; } = new List<UserDeviceToken>();
}
