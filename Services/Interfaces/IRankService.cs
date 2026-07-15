using BookingHub.Api.Dtos.Member;
using BookingHub.Api.Dtos.Rank;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania rangami organizacyjnymi.
/// </summary>
public interface IRankService
{
    /// <summary>Pobiera wszystkie rangi organizacji z liczbą członków.</summary>
    Task<IReadOnlyList<RankSummaryResponse>> GetAllAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły rangi.</summary>
    Task<RankDetailResponse> GetByIdAsync(Guid rankId, CancellationToken ct = default);

    /// <summary>Pobiera stronicowaną listę członków z daną rangą.</summary>
    Task<PagedResult<MemberSummaryResponse>> GetMembersAsync(Guid rankId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Tworzy nową rangę w organizacji.</summary>
    Task<RankDetailResponse> CreateAsync(Guid organizationId, CreateRankRequest request, CancellationToken ct = default);

    /// <summary>Aktualizuje dane rangi (nazwa, kolor).</summary>
    Task<RankDetailResponse> UpdateAsync(Guid rankId, UpdateRankRequest request, CancellationToken ct = default);

    /// <summary>Usuwa rangę (soft delete). Członkowie tracą przypisanie do rangi.</summary>
    Task DeleteAsync(Guid rankId, CancellationToken ct = default);

    /// <summary>
    /// Przypisuje lub usuwa rangę członka.
    /// rankId = null → usuwa rangę.
    /// Waliduje przynależność rangi do tej samej organizacji co członek.
    /// </summary>
    Task<MemberDetailResponse> SetMemberRankAsync(Guid organizationId, Guid memberId, Guid? rankId, CancellationToken ct = default);
}
