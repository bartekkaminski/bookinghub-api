using BookingHub.Api.Dtos.Availability;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania dostępnością uczestników i trenerów.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>Pobiera wszystkie sloty dostępności dla danego członka.</summary>
    Task<IReadOnlyList<AvailabilitySlotResponse>> GetByMemberAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>
    /// Pobiera sloty dostępności aktywne w danym dniu (z filtrowaniem ValidFrom/ValidTo).
    /// </summary>
    Task<IReadOnlyList<AvailabilitySlotResponse>> GetByMemberAndDateAsync(Guid memberId, DateOnly date, CancellationToken ct = default);

    /// <summary>Dodaje nowy slot dostępności dla członka.</summary>
    Task<AvailabilitySlotResponse> AddSlotAsync(Guid memberId, AddAvailabilitySlotRequest request, CancellationToken ct = default);

    /// <summary>Aktualizuje istniejący slot dostępności.</summary>
    Task<AvailabilitySlotResponse> UpdateSlotAsync(Guid slotId, UpdateAvailabilitySlotRequest request, CancellationToken ct = default);

    /// <summary>Usuwa slot dostępności.</summary>
    Task DeleteSlotAsync(Guid slotId, CancellationToken ct = default);

    /// <summary>
    /// Sprawdza dostępność wielu członków w podanym przedziale dat/czasu.
    /// Używane przy planowaniu zajęć do weryfikacji dostępności trenerów.
    /// </summary>
    Task<AvailabilityCheckResponse> CheckAvailabilityAsync(IReadOnlyList<Guid> memberIds, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Zwraca scalony grafik członka (sloty dostępności + zajęcia) dla podanego zakresu dat.
    /// Zwraca TYLKO bloki Available i Busy.
    /// Obszary poza dostępnością (Unavailable) są pomijane — frontend renderuje je jako puste tło.
    /// Typowy zakres: 1 dzień (widok dnia). Maksymalny zakres: 90 dni.
    /// </summary>
    Task<IReadOnlyList<MemberScheduleResponse>> GetMemberScheduleAsync(
        Guid memberId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
