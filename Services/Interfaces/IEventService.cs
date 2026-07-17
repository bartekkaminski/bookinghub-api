using BookingHub.Api.Dtos.Event;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania zajęciami (jednorazowymi i cyklicznymi).
/// </summary>
public interface IEventService
{
    /// <summary>Pobiera stronicowaną listę zajęć w organizacji.</summary>
    Task<PagedResult<EventSummaryResponse>> GetPagedAsync(Guid organizationId, EventFilterParams filter, CancellationToken ct = default);

    /// <summary>
    /// Pobiera zajęcia w przedziale dat w formacie zoptymalizowanym pod widok kalendarza.
    /// </summary>
    Task<IReadOnlyList<EventCalendarResponse>> GetCalendarAsync(Guid organizationId, CalendarRequest request, CancellationToken ct = default);

    /// <summary>
    /// Pobiera zajęcia konkretnego uczestnika (widok jego harmonogramu/kalendarza).
    /// </summary>
    Task<IReadOnlyList<EventCalendarResponse>> GetCalendarForMemberAsync(Guid memberId, CalendarRequest request, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły zajęć wraz z trenerami, zapisami i wpisami.</summary>
    Task<EventDetailResponse> GetByIdAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>Tworzy nowe jednorazowe zajęcia.</summary>
    /// <param name="creatorMemberId">Członek org tworzący zajęcia — jeśli ma rolę Trainer, zostanie auto-przypisany.</param>
    Task<EventDetailResponse> CreateAsync(
        Guid organizationId, CreateEventRequest request, Guid? creatorMemberId = null, CancellationToken ct = default);

    /// <summary>
    /// Tworzy cykl zajęć: wiele Event z tym samym SeriesGroupId
    /// dla dni tygodnia w podanym zakresie dat.
    /// </summary>
    /// <param name="creatorMemberId">Członek org tworzący cykl — jeśli ma rolę Trainer, zostanie auto-przypisany do każdego wystąpienia.</param>
    Task<CreateRecurringEventsResponse> CreateRecurringAsync(
        Guid organizationId, CreateRecurringEventsRequest request, Guid? creatorMemberId = null, CancellationToken ct = default);

    /// <summary>Pobiera wszystkie zajęcia należące do danego cyklu (SeriesGroupId).</summary>
    Task<IReadOnlyList<EventSummaryResponse>> GetBySeriesGroupAsync(
        Guid organizationId, Guid seriesGroupId, CancellationToken ct = default);

    /// <summary>
    /// Odwołuje wszystkie przyszłe zaplanowane zajęcia w cyklu (StartTime &gt;= now, Status = Scheduled).
    /// </summary>
    Task<CancelFutureInSeriesGroupResponse> CancelFutureInSeriesGroupAsync(
        Guid organizationId, Guid seriesGroupId, CancelFutureInSeriesGroupRequest request, CancellationToken ct = default);

    /// <summary>Aktualizuje dane zajęć (dozwolone tylko gdy status = Scheduled).</summary>
    Task<EventDetailResponse> UpdateAsync(Guid eventId, UpdateEventRequest request, CancellationToken ct = default);

    /// <summary>
    /// Odwołuje zajęcia (zmiana statusu na Cancelled).
    /// Opcjonalnie wysyła wiadomość do uczestników.
    /// </summary>
    Task<EventDetailResponse> CancelAsync(Guid eventId, CancelEventRequest request, CancellationToken ct = default);

    /// <summary>
    /// Kończy zajęcia (zmiana statusu na Completed).
    /// Powoduje zmianę statusu wszystkich aktywnych zapisów na Attended.
    /// </summary>
    Task<EventDetailResponse> CompleteAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>Usuwa zajęcia (soft delete). Tylko gdy status = Scheduled i brak zapisów.</summary>
    Task DeleteAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>Przypisuje trenera do zajęć.</summary>
    Task<EventDetailResponse> AssignTrainerAsync(Guid eventId, Guid trainerMemberId, CancellationToken ct = default);

    /// <summary>Usuwa przypisanie trenera z zajęć.</summary>
    Task<EventDetailResponse> RemoveTrainerAsync(Guid eventId, Guid trainerMemberId, CancellationToken ct = default);
}
