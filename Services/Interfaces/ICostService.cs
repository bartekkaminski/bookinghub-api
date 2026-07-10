using BookingHub.Api.Dtos.Cost;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania stawkami i kalkulacji kosztów.
/// </summary>
public interface ICostService
{
    // ── Group Cost Rates ──────────────────────────────────────────────────────

    /// <summary>Pobiera historię stawek miesięcznych dla grupy.</summary>
    Task<IReadOnlyList<GroupCostRateResponse>> GetGroupRatesAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>Pobiera aktualnie obowiązującą stawkę grupy.</summary>
    Task<GroupCostRateResponse?> GetCurrentGroupRateAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>
    /// Dodaje nową stawkę miesięczną dla grupy.
    /// Jeśli istnieje aktywna stawka bez ValidTo — zamykana jest automatycznie dzień przed ValidFrom nowej.
    /// </summary>
    Task<GroupCostRateResponse> AddGroupRateAsync(Guid groupId, AddGroupCostRateRequest request, CancellationToken ct = default);

    /// <summary>Ręcznie zamyka aktywną stawkę (ustawia ValidTo).</summary>
    Task<GroupCostRateResponse> CloseGroupRateAsync(Guid rateId, CloseGroupCostRateRequest request, CancellationToken ct = default);

    /// <summary>Usuwa stawkę grupy (tylko jeśli nie zamknięta i brak rozliczeń).</summary>
    Task DeleteGroupRateAsync(Guid rateId, CancellationToken ct = default);

    // ── Trainer Session Rates ─────────────────────────────────────────────────

    /// <summary>Pobiera historię stawek godzinowych trenera.</summary>
    Task<IReadOnlyList<TrainerSessionRateResponse>> GetTrainerRatesAsync(Guid trainerMemberId, CancellationToken ct = default);

    /// <summary>Pobiera aktualnie obowiązującą stawkę trenera.</summary>
    Task<TrainerSessionRateResponse?> GetCurrentTrainerRateAsync(Guid trainerMemberId, CancellationToken ct = default);

    /// <summary>
    /// Dodaje nową stawkę godzinową trenera.
    /// Jeśli istnieje aktywna stawka — zamykana automatycznie dzień przed ValidFrom nowej.
    /// </summary>
    Task<TrainerSessionRateResponse> AddTrainerRateAsync(Guid trainerMemberId, AddTrainerSessionRateRequest request, CancellationToken ct = default);

    /// <summary>Ręcznie zamyka aktywną stawkę trenera.</summary>
    Task<TrainerSessionRateResponse> CloseTrainerRateAsync(Guid rateId, CloseTrainerSessionRateRequest request, CancellationToken ct = default);

    /// <summary>Usuwa stawkę trenera (tylko jeśli nie zamknięta i brak rozliczeń).</summary>
    Task DeleteTrainerRateAsync(Guid rateId, CancellationToken ct = default);

    // ── Billing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Kalkuluje miesięczny rachunek uczestnika:
    /// - składki za grupy (stawka miesięczna × proporcja miesiąca)
    /// - koszty zajęć indywidualnych (stawka trenera × czas ÷ liczba uczestników)
    /// - jednorazowe opłaty za obozy / eventy
    /// </summary>
    Task<MemberMonthlyBillResponse> CalculateMemberMonthlyBillAsync(Guid memberId, int year, int month, CancellationToken ct = default);

    /// <summary>
    /// Kalkuluje rachunki wszystkich aktywnych uczestników organizacji za dany miesiąc.
    /// </summary>
    Task<IReadOnlyList<MemberMonthlyBillResponse>> CalculateOrganizationMonthlyBillAsync(Guid organizationId, int year, int month, CancellationToken ct = default);
}
