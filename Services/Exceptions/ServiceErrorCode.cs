namespace BookingHub.Api.Services.Exceptions;

/// <summary>
/// Kody błędów domenowych zwracanych przez serwisy.
/// Kontroler tłumaczy te kody na odpowiednie statusy HTTP.
/// </summary>
public enum ServiceErrorCode
{
    // ── Ogólne ────────────────────────────────────────────────────────────────
    /// <summary>Zasób nie został znaleziony.</summary>
    NotFound,
    /// <summary>Brak uwierzytelnienia lub nieprawidłowy token.</summary>
    Unauthorized,
    /// <summary>Brak uprawnień do wykonania operacji.</summary>
    Forbidden,
    /// <summary>Konflikt — zasób już istnieje lub narusza unikalność.</summary>
    Conflict,
    /// <summary>Nieprawidłowe dane wejściowe lub naruszenie reguły biznesowej.</summary>
    ValidationError,

    // ── User / Konto ───────────────────────────────────────────────────────────
    /// <summary>Adres e-mail jest już zajęty przez innego użytkownika.</summary>
    EmailAlreadyTaken,
    /// <summary>Para (ExternalId, AuthProvider) jest już zarejestrowana.</summary>
    ExternalIdAlreadyTaken,
    /// <summary>Konto jest nieaktywne — dostęp zablokowany globalnie.</summary>
    AccountInactive,

    // ── Organization ──────────────────────────────────────────────────────────
    /// <summary>Nazwa organizacji jest już zajęta.</summary>
    OrganizationNameTaken,
    /// <summary>Osoba jest już członkiem tej organizacji.</summary>
    AlreadyMember,
    /// <summary>Osoba nie jest członkiem tej organizacji.</summary>
    NotMember,

    // ── Role ──────────────────────────────────────────────────────────────────
    /// <summary>Członek ma już tę rolę w organizacji.</summary>
    RoleAlreadyAssigned,
    /// <summary>Próba usunięcia ostatniej roli Admina w organizacji.</summary>
    CannotRemoveLastAdmin,
    /// <summary>Operacja wymaga roli Trenera, a członek jej nie ma.</summary>
    NotATrainer,
    /// <summary>Operacja wymaga roli Uczestnika, a członek jej nie ma.</summary>
    NotAParticipant,

    // ── Group ─────────────────────────────────────────────────────────────────
    /// <summary>Nazwa grupy jest już zajęta w tej organizacji.</summary>
    GroupNameTaken,
    /// <summary>Uczestnik jest już w tej grupie.</summary>
    MemberAlreadyInGroup,
    /// <summary>Zespół jest już przypisany do tej grupy.</summary>
    TeamAlreadyInGroup,

    // ── Team ──────────────────────────────────────────────────────────────────
    /// <summary>Uczestnik jest już w tym zespole.</summary>
    MemberAlreadyInTeam,
    /// <summary>Trener jest już przypisany do tego zespołu.</summary>
    TrainerAlreadyAssignedToTeam,
    /// <summary>Trener jest już przypisany do tego uczestnika.</summary>
    TrainerAlreadyAssignedToParticipant,

    // ── Location ──────────────────────────────────────────────────────────────
    /// <summary>Nazwa lokalizacji jest już zajęta w tej organizacji.</summary>
    LocationNameTaken,

    // ── Event ─────────────────────────────────────────────────────────────────
    /// <summary>Zajęcia zostały odwołane — nie można wykonać tej operacji.</summary>
    EventCancelled,
    /// <summary>Zajęcia zostały zakończone — nie można wykonać tej operacji.</summary>
    EventCompleted,
    /// <summary>Trener jest już przypisany do tych zajęć.</summary>
    TrainerAlreadyAssignedToEvent,
    /// <summary>Data zakończenia zajęć musi być późniejsza niż data rozpoczęcia.</summary>
    InvalidEventDateRange,

    // ── Enrollment ────────────────────────────────────────────────────────────
    /// <summary>Uczestnik jest już zapisany na te zajęcia.</summary>
    MemberAlreadyEnrolled,
    /// <summary>Zespół jest już zapisany na te zajęcia.</summary>
    TeamAlreadyEnrolled,
    /// <summary>Zapis nie istnieje lub jest już odwołany.</summary>
    EnrollmentNotActive,

    // ── Cancellation ──────────────────────────────────────────────────────────
    /// <summary>Dla tego zapisu istnieje już oczekujący wniosek o odwołanie.</summary>
    CancellationRequestAlreadyPending,
    /// <summary>Wniosek o odwołanie nie jest w stanie Pending — nie można go rozpatrzyć ponownie.</summary>
    CancellationRequestNotPending,

    // ── Message ───────────────────────────────────────────────────────────────
    /// <summary>Lista odbiorców wiadomości jest pusta.</summary>
    MessageNoRecipients,
    /// <summary>Wiadomość nie należy do nadawcy — nie można odpowiedzieć.</summary>
    MessageNotFound,

    // ── Cost / Rates ──────────────────────────────────────────────────────────
    /// <summary>Istnieje już aktywna stawka (ValidTo IS NULL) — zamknij poprzednią przed dodaniem nowej.</summary>
    ActiveRateAlreadyExists,
    /// <summary>Przedział dat stawki jest nieprawidłowy (ValidFrom >= ValidTo).</summary>
    InvalidRateDateRange,

    // ── Kinde / Integracja zewnętrzna ─────────────────────────────────────────
    /// <summary>Kinde Management API zwróciło błąd.</summary>
    KindeApiError,

    // ── Infrastruktura ────────────────────────────────────────────────────────
    /// <summary>Błąd zapisu do bazy danych po udanej operacji w zewnętrznym serwisie.</summary>
    DatabaseError,
}
