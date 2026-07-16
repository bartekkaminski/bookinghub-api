using BookingHub.Api.Dtos.Discipline;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania dyscyplinami organizacyjnymi.
/// </summary>
public interface IDisciplineService
{
    /// <summary>Pobiera wszystkie dyscypliny organizacji z liczbą rang.</summary>
    Task<IReadOnlyList<DisciplineSummaryResponse>> GetAllAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły dyscypliny. Rzuca NotFound, jeśli nie należy do organizacji.</summary>
    Task<DisciplineDetailResponse> GetByIdAsync(Guid organizationId, Guid disciplineId, CancellationToken ct = default);

    /// <summary>Tworzy nową dyscyplinę w organizacji.</summary>
    Task<DisciplineDetailResponse> CreateAsync(Guid organizationId, CreateDisciplineRequest request, CancellationToken ct = default);

    /// <summary>Aktualizuje dane dyscypliny (nazwa, kolor). Rzuca NotFound, jeśli nie należy do organizacji.</summary>
    Task<DisciplineDetailResponse> UpdateAsync(Guid organizationId, Guid disciplineId, UpdateDisciplineRequest request, CancellationToken ct = default);

    /// <summary>
    /// Usuwa dyscyplinę (soft delete). Blokowane, jeśli dyscyplina ma jeszcze jakieś rangi —
    /// admin musi je najpierw usunąć. Rzuca NotFound, jeśli nie należy do organizacji.
    /// </summary>
    Task DeleteAsync(Guid organizationId, Guid disciplineId, CancellationToken ct = default);
}
