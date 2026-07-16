using BookingHub.Api.Dtos.Member;
using BookingHub.Api.Dtos.Rank;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania rangami organizacyjnymi.
/// </summary>
public interface IRankService
{
    /// <summary>Pobiera wszystkie rangi dyscypliny z liczbą członków. Rzuca NotFound, jeśli dyscyplina nie należy do organizacji.</summary>
    Task<IReadOnlyList<RankSummaryResponse>> GetAllAsync(Guid organizationId, Guid disciplineId, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły rangi. Rzuca NotFound, jeśli ranga nie należy do organizacji/dyscypliny.</summary>
    Task<RankDetailResponse> GetByIdAsync(Guid organizationId, Guid disciplineId, Guid rankId, CancellationToken ct = default);

    /// <summary>Pobiera stronicowaną listę członków z daną rangą.</summary>
    Task<PagedResult<MemberSummaryResponse>> GetMembersAsync(Guid organizationId, Guid disciplineId, Guid rankId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Tworzy nową rangę w ramach dyscypliny.</summary>
    Task<RankDetailResponse> CreateAsync(Guid organizationId, Guid disciplineId, CreateRankRequest request, CancellationToken ct = default);

    /// <summary>Aktualizuje dane rangi (nazwa, kolor). Rzuca NotFound, jeśli ranga nie należy do organizacji/dyscypliny.</summary>
    Task<RankDetailResponse> UpdateAsync(Guid organizationId, Guid disciplineId, Guid rankId, UpdateRankRequest request, CancellationToken ct = default);

    /// <summary>Usuwa rangę (soft delete). Członkowie tracą przypisanie do rangi w tej dyscyplinie.</summary>
    Task DeleteAsync(Guid organizationId, Guid disciplineId, Guid rankId, CancellationToken ct = default);

    /// <summary>
    /// Przypisuje lub usuwa rangę członka w ramach konkretnej dyscypliny.
    /// rankId = null → usuwa rangę członka w tej dyscyplinie.
    /// Waliduje przynależność dyscypliny i rangi do tej samej organizacji co członek.
    /// </summary>
    Task<MemberDetailResponse> SetMemberRankAsync(
        Guid organizationId, Guid memberId, Guid disciplineId, Guid? rankId, CancellationToken ct = default);
}
