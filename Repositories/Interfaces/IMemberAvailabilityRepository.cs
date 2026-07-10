using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium slotów dostępności trenerów i uczestników (MemberAvailability).
/// </summary>
public interface IMemberAvailabilityRepository : IBaseRepository<MemberAvailability>
{
    /// <summary>
    /// Pobiera stronicowaną listę slotów dostępności z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<MemberAvailability>> GetPagedAsync(MemberAvailabilityFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie sloty dostępności danego członka organizacji.
    /// </summary>
    Task<IReadOnlyList<MemberAvailability>> GetByMemberAsync(Guid organizationMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera sloty dostępności danego członka obowiązujące w podanym dniu.
    /// Uwzględnia ValidFrom i ValidTo.
    /// </summary>
    Task<IReadOnlyList<MemberAvailability>> GetByMemberOnDateAsync(Guid organizationMemberId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera sloty dostępności danego członka na konkretny dzień tygodnia.
    /// </summary>
    Task<IReadOnlyList<MemberAvailability>> GetByMemberAndDayAsync(Guid organizationMemberId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera dostępność grupy członków w podanym przedziale tygodniowym — np. do planowania zajęć.
    /// </summary>
    Task<IReadOnlyList<MemberAvailability>> GetByMembersOnDayAsync(IEnumerable<Guid> organizationMemberIds, DayOfWeek dayOfWeek, DateOnly? onDate = null, CancellationToken cancellationToken = default);
}
