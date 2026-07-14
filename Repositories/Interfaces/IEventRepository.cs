using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium zajęć (Events) — konkretnych terminów w kalendarzu.
/// </summary>
public interface IEventRepository : IBaseRepository<Event>
{
    /// <summary>
    /// Pobiera zajęcia wraz z trenerami, zapisami uczestników i zespołów.
    /// </summary>
    Task<Event?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera zajęcia wraz z trenerami (EventTrainer → OrganizationMember → Person).
    /// </summary>
    Task<Event?> GetWithTrainersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera zajęcia wraz z zapisami (EventEnrollment → OrganizationMember → Person).
    /// </summary>
    Task<Event?> GetWithEnrollmentsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę zajęć z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<Event>> GetPagedAsync(EventFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera kalendarz zajęć organizacji w podanym przedziale dat (do widoku kalendarza).
    /// Zawiera trenerów i grupę — potrzebne do renderowania bloczków.
    /// </summary>
    Task<IReadOnlyList<Event>> GetCalendarAsync(Guid organizationId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera kalendarz zajęć dla konkretnego uczestnika w przedziale dat.
    /// Łączy zapisy indywidualne i zapisy przez zespoły.
    /// </summary>
    Task<IReadOnlyList<Event>> GetCalendarForMemberAsync(Guid organizationMemberId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie zajęcia należące do danej serii.
    /// </summary>
    Task<IReadOnlyList<Event>> GetBySeriesAsync(Guid eventSeriesId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera nadchodzące zajęcia organizacji (StartTime &gt;= now), ograniczone do podanej liczby.
    /// </summary>
    Task<IReadOnlyList<Event>> GetUpcomingAsync(Guid organizationId, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zmienia status zajęć. Zwraca false jeśli zajęcia nie istnieją.
    /// </summary>
    Task<bool> SetStatusAsync(Guid eventId, EventStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera zajęcia prowadzone przez danego trenera w przedziale dat.
    /// </summary>
    Task<IReadOnlyList<Event>> GetByTrainerAsync(Guid trainerMemberId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy trener jest już przypisany do zajęć.
    /// </summary>
    Task<bool> IsTrainerAssignedAsync(Guid eventId, Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Przypisuje trenera do zajęć (tworzy EventTrainer).
    /// </summary>
    Task AddTrainerAsync(Guid eventId, Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa trenera z zajęć.
    /// </summary>
    Task RemoveTrainerAsync(Guid eventId, Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca liczbę zajęć przypisanych do danej grupy.
    /// </summary>
    Task<int> CountByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera zajęcia przypisane do danej lokalizacji w przedziale dat.
    /// Zawiera szczegóły potrzebne do widoku harmonogramu sali:
    /// grupę, serię (dla koloru), zapisy indywidualne i zespołowe z liczebnościami.
    /// </summary>
    Task<IReadOnlyList<Event>> GetByLocationAndRangeAsync(
        Guid locationId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
