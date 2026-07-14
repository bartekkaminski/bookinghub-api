using BookingHub.Api.Models;

namespace BookingHub.Api.Dtos.Location;

/// <summary>Poziom zajętości sali w danym dniu.</summary>
public enum LocationOccupancy
{
    /// <summary>Brak zajęć.</summary>
    None,
    /// <summary>Zajęcia zajmują mniej niż 8 godzin dnia (unia interwałów).</summary>
    Partial,
    /// <summary>Zajęcia zajmują co najmniej 8 godzin dnia.</summary>
    Full,
}

/// <summary>Podsumowanie zajętości sali dla jednego dnia (widok miesięczny).</summary>
public sealed class LocationDaySummary
{
    public DateOnly Date { get; set; }
    public int EventCount { get; set; }
    /// <summary>Łączna liczba godzin pokrytych przez zajęcia (unia interwałów — bez podwójnego liczenia).</summary>
    public double CoveredHours { get; set; }
    public LocationOccupancy Occupancy { get; set; }
}

/// <summary>Odpowiedź widoku miesięcznego harmonogramu sali.</summary>
public sealed class LocationMonthSummaryResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public IReadOnlyList<LocationDaySummary> Days { get; set; } = [];
}

/// <summary>Informacja o zespole zapisanym na zajęcia (bez imion członków — prywatność).</summary>
public sealed class LocationDayTeamInfo
{
    public Guid TeamId { get; set; }
    public string? TeamName { get; set; }
    /// <summary>Liczba aktywnych członków zespołu.</summary>
    public int MemberCount { get; set; }
}

/// <summary>Dane zajęć w widoku dziennym harmonogramu sali.</summary>
public sealed class LocationDayEventResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public EventType EventType { get; set; }
    public EventStatus Status { get; set; }
    /// <summary>Kolor zajęć (własny → serii → grupy → szary domyślny).</summary>
    public string Color { get; set; } = "#9CA3AF";
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    /// <summary>
    /// Liczba aktywnych zapisów indywidualnych (Enrolled / PendingApproval / Attended).
    /// Bez imion uczestników — prywatność.
    /// </summary>
    public int IndividualCount { get; set; }
    public IReadOnlyList<LocationDayTeamInfo> Teams { get; set; } = [];
}

/// <summary>Odpowiedź widoku dziennego harmonogramu sali.</summary>
public sealed class LocationDayScheduleResponse
{
    public DateOnly Date { get; set; }
    public IReadOnlyList<LocationDayEventResponse> Events { get; set; } = [];
}
