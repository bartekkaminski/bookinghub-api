namespace BookingHub.Api.Settings;

/// <summary>
/// Limity dotyczące organizacji — konfigurowane przez appsettings.json.
/// Sekcja: "OrganizationLimits"
/// </summary>
public sealed class OrganizationLimits
{
    public const string SectionName = "OrganizationLimits";

    /// <summary>
    /// Maksymalna liczba organizacji, które jedna osoba może utworzyć.
    /// 0 lub mniej = brak limitu.
    /// </summary>
    public int MaxOrganizationsPerCreator { get; init; } = 1;
}
