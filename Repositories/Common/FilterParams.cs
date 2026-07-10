using BookingHub.Api.Models;

namespace BookingHub.Api.Repositories.Common;

/// <summary>
/// Bazowe parametry stronicowania i sortowania wspólne dla wszystkich zapytań listowych.
/// </summary>
public abstract class FilterParams
{
    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>Numer strony (min. 1).</summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>Rozmiar strony (1–100).</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 100 ? 20 : value;
    }

    /// <summary>Nazwa pola sortowania (case-insensitive).</summary>
    public string? SortBy { get; set; }

    /// <summary>Kierunek sortowania: true = malejąco, false = rosnąco (domyślnie).</summary>
    public bool SortDescending { get; set; } = false;
}

// ─────────────────────────────────────────────────────────────────────────────
// User
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy użytkowników logowania.</summary>
public sealed class UserFilterParams : FilterParams
{
    /// <summary>Filtr po fragmencie adresu e-mail (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Filtr po stanie konta: true = tylko aktywne, false = tylko nieaktywne, null = wszystkie.</summary>
    public bool? IsActive { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Person
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy profili osób.</summary>
public sealed class PersonFilterParams : FilterParams
{
    /// <summary>Filtr po fragmencie imienia lub nazwiska (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filtr po posiadaniu konta logowania: true = tylko osoby z kontem, false = bez konta, null = wszyscy.
    /// </summary>
    public bool? HasAccount { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Organization
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy organizacji.</summary>
public sealed class OrganizationFilterParams : FilterParams
{
    /// <summary>Filtr po fragmencie nazwy organizacji (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>
    /// Gdy ustawiony — zwraca tylko organizacje, których członkiem jest dana osoba.
    /// Ustawiany server-side przez kontroler — klient nie może go nadpisać.
    /// </summary>
    public Guid? PersonId { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// OrganizationMember
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy członków organizacji.</summary>
public sealed class OrganizationMemberFilterParams : FilterParams
{
    /// <summary>Filtr po identyfikatorze organizacji (wymagany w kontekście org-scoped API).</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Filtr po fragmencie imienia, nazwiska lub displayName (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Filtr po roli w organizacji.</summary>
    public MemberRole? Role { get; set; }

    /// <summary>Filtr po aktywności członkostwa: true = aktywni, false = nieaktywni, null = wszyscy.</summary>
    public bool? IsActive { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Group
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy grup zajęciowych.</summary>
public sealed class GroupFilterParams : FilterParams
{
    /// <summary>Filtr po organizacji.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Filtr po fragmencie nazwy grupy (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Filtr po aktywności grupy: true = aktywne, false = nieaktywne, null = wszystkie.</summary>
    public bool? IsActive { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Team
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy zespołów (pary, formacje, drużyny).</summary>
public sealed class TeamFilterParams : FilterParams
{
    /// <summary>Filtr po organizacji.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Filtr po fragmencie nazwy zespołu (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Filtr po aktywności zespołu.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Filtr po identyfikatorze grupy — zwraca zespoły przypisane do danej grupy.</summary>
    public Guid? GroupId { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Location
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy lokalizacji.</summary>
public sealed class LocationFilterParams : FilterParams
{
    /// <summary>Filtr po organizacji.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Filtr po fragmencie nazwy lub adresu (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Filtr po aktywności lokalizacji.</summary>
    public bool? IsActive { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// EventSeries
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy serii cyklicznych zajęć.</summary>
public sealed class EventSeriesFilterParams : FilterParams
{
    /// <summary>Filtr po organizacji.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Filtr po fragmencie tytułu serii (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Filtr po domyślnej grupie przypisanej do serii.</summary>
    public Guid? DefaultGroupId { get; set; }

    /// <summary>Filtr po domyślnej lokalizacji serii.</summary>
    public Guid? DefaultLocationId { get; set; }

    /// <summary>Filtr po domyślnym typie zajęć serii.</summary>
    public EventType? DefaultEventType { get; set; }

    /// <summary>Filtr po aktywności serii.</summary>
    public bool? IsActive { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Event
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy zajęć (wydarzeń).</summary>
public sealed class EventFilterParams : FilterParams
{
    /// <summary>Filtr po organizacji.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Filtr po serii — zwraca tylko zajęcia należące do danej serii.</summary>
    public Guid? EventSeriesId { get; set; }

    /// <summary>Filtr po grupie przypisanej do zajęć.</summary>
    public Guid? GroupId { get; set; }

    /// <summary>Filtr po lokalizacji zajęć.</summary>
    public Guid? LocationId { get; set; }

    /// <summary>Filtr po typie zajęć.</summary>
    public EventType? EventType { get; set; }

    /// <summary>Filtr po statusie zajęć.</summary>
    public EventStatus? Status { get; set; }

    /// <summary>Filtr: zajęcia zaczynające się od tej daty/czasu (UTC, włącznie).</summary>
    public DateTime? StartFrom { get; set; }

    /// <summary>Filtr: zajęcia zaczynające się do tej daty/czasu (UTC, włącznie).</summary>
    public DateTime? StartTo { get; set; }

    /// <summary>Filtr po fragmencie tytułu zajęć (case-insensitive).</summary>
    public string? Search { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// EventEnrollment
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy zapisów indywidualnych na zajęcia.</summary>
public sealed class EventEnrollmentFilterParams : FilterParams
{
    /// <summary>Filtr po zajęciach.</summary>
    public Guid? EventId { get; set; }

    /// <summary>Filtr po członku organizacji (uczestniku).</summary>
    public Guid? OrganizationMemberId { get; set; }

    /// <summary>Filtr po statusie zapisu.</summary>
    public EventEnrollmentStatus? Status { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// EventTeamEnrollment
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy zapisów zespołów na zajęcia.</summary>
public sealed class EventTeamEnrollmentFilterParams : FilterParams
{
    /// <summary>Filtr po zajęciach.</summary>
    public Guid? EventId { get; set; }

    /// <summary>Filtr po zespole.</summary>
    public Guid? TeamId { get; set; }

    /// <summary>Filtr po statusie zapisu.</summary>
    public EventEnrollmentStatus? Status { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// CancellationRequest
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy wniosków o odwołanie zapisu.</summary>
public sealed class CancellationRequestFilterParams : FilterParams
{
    /// <summary>Filtr po organizacji — ustawiany server-side, użytkownik nie może go nadpisać.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Filtr po zapisie na zajęcia.</summary>
    public Guid? EventEnrollmentId { get; set; }

    /// <summary>Filtr po uczestniku składającym wniosek.</summary>
    public Guid? RequestedByMemberId { get; set; }

    /// <summary>Filtr po statusie wniosku.</summary>
    public CancellationStatus? Status { get; set; }

    /// <summary>Filtr: wnioski złożone od tej daty (UTC, włącznie).</summary>
    public DateTime? RequestedFrom { get; set; }

    /// <summary>Filtr: wnioski złożone do tej daty (UTC, włącznie).</summary>
    public DateTime? RequestedTo { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Message
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy wiadomości.</summary>
public sealed class MessageFilterParams : FilterParams
{
    /// <summary>Filtr po organizacji.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Filtr po nadawcy wiadomości.</summary>
    public Guid? SenderMemberId { get; set; }

    /// <summary>Filtr: wiadomości wysłane od tej daty (UTC, włącznie).</summary>
    public DateTime? SentFrom { get; set; }

    /// <summary>Filtr: wiadomości wysłane do tej daty (UTC, włącznie).</summary>
    public DateTime? SentTo { get; set; }

    /// <summary>Filtr po fragmencie tematu (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Filtr po typie wiadomości: true = tylko automatyczne, false = tylko ręczne, null = wszystkie.</summary>
    public bool? IsAutomatic { get; set; }

    /// <summary>
    /// Filtr po powiązanych zajęciach — zwraca wiadomości dotyczące konkretnych zajęć.
    /// </summary>
    public Guid? RelatedEventId { get; set; }

    /// <summary>
    /// Filtr po wątku — zwraca odpowiedzi na daną wiadomość nadrzędną.
    /// Null = pobierz tylko wiadomości będące korzeniami wątków (ParentMessageId IS NULL).
    /// Guid = pobierz odpowiedzi na tę wiadomość.
    /// </summary>
    public Guid? ParentMessageId { get; set; }

    /// <summary>Gdy true — zwraca tylko wiadomości bez rodzica (korzenie wątków).</summary>
    public bool OnlyRootMessages { get; set; } = false;
}

// ─────────────────────────────────────────────────────────────────────────────
// MemberAvailability
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy slotów dostępności członków.</summary>
public sealed class MemberAvailabilityFilterParams : FilterParams
{
    /// <summary>Filtr po członku organizacji.</summary>
    public Guid? OrganizationMemberId { get; set; }

    /// <summary>Filtr po dniu tygodnia.</summary>
    public DayOfWeek? DayOfWeek { get; set; }

    /// <summary>
    /// Filtr po dacie — zwraca sloty obowiązujące w podanym dniu
    /// (ValidFrom &lt;= date &amp;&amp; (ValidTo == null || ValidTo &gt;= date)).
    /// </summary>
    public DateOnly? ActiveOnDate { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// GroupCostRate
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania historii stawek miesięcznych grupy.</summary>
public sealed class GroupCostRateFilterParams : FilterParams
{
    /// <summary>Filtr po grupie zajęciowej.</summary>
    public Guid? GroupId { get; set; }

    /// <summary>Filtr po walucie, np. "PLN".</summary>
    public string? Currency { get; set; }

    /// <summary>Filtr: stawki obowiązujące od tej daty (włącznie).</summary>
    public DateOnly? ValidFrom { get; set; }

    /// <summary>Filtr: stawki obowiązujące do tej daty (włącznie). Null = tylko aktualne.</summary>
    public DateOnly? ValidTo { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// TrainerSessionRate
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania historii stawek godzinowych trenerów.</summary>
public sealed class TrainerSessionRateFilterParams : FilterParams
{
    /// <summary>Filtr po trenerze (OrganizationMemberId z rolą Trainer).</summary>
    public Guid? TrainerMemberId { get; set; }

    /// <summary>Filtr po walucie.</summary>
    public string? Currency { get; set; }

    /// <summary>Filtr: stawki obowiązujące od tej daty (włącznie).</summary>
    public DateOnly? ValidFrom { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// ParentChildRelation
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parametry filtrowania listy relacji rodzic–dziecko.</summary>
public sealed class ParentChildRelationFilterParams : FilterParams
{
    /// <summary>Filtr po rodzicu/opiekunie.</summary>
    public Guid? ParentPersonId { get; set; }

    /// <summary>Filtr po dziecku.</summary>
    public Guid? ChildPersonId { get; set; }
}
