using BookingHub.Api.Dtos.Member;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania członkostwami w organizacjach i rolami.
/// </summary>
public interface IOrganizationMemberService
{
    /// <summary>Pobiera stronicowaną listę członków organizacji.</summary>
    Task<PagedResult<MemberSummaryResponse>> GetPagedAsync(Guid organizationId, OrganizationMemberFilterParams filter, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły członkostwa.</summary>
    Task<MemberDetailResponse> GetByIdAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>Pobiera listę wszystkich aktywnych członków organizacji (do selectów).</summary>
    Task<IReadOnlyList<MemberSummaryResponse>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Pobiera listę aktywnych trenerów w organizacji.</summary>
    Task<IReadOnlyList<MemberSummaryResponse>> GetTrainersAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Pobiera listę aktywnych uczestników w organizacji.</summary>
    Task<IReadOnlyList<MemberSummaryResponse>> GetParticipantsAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Dodaje istniejącą osobę (Person) jako nowego członka organizacji.
    /// </summary>
    Task<MemberDetailResponse> AddMemberAsync(Guid organizationId, AddMemberRequest request, CancellationToken ct = default);

    /// <summary>
    /// Tworzy konto Kinde + Person + OrganizationMember w jednym kroku.
    /// Używane przez admina organizacji.
    /// </summary>
    Task<MemberDetailResponse> CreateMemberWithAccountAsync(Guid organizationId, CreateMemberWithAccountRequest request, CancellationToken ct = default);

    /// <summary>
    /// Tworzy profil Person (bez konta Kinde/logowania) i dodaje go jako członka organizacji.
    /// Używane np. dla dzieci które nie mają własnego konta.
    /// </summary>
    Task<MemberDetailResponse> CreateMemberProfileAsync(Guid organizationId, CreateMemberProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Przypisuje konto logowania (Kinde + User) do istniejącego profilu bez konta.
    /// Rzuca <see cref="ServiceException"/> gdy profil już ma konto lub email jest zajęty.
    /// </summary>
    Task<MemberDetailResponse> AttachAccountAsync(Guid organizationId, Guid memberId, AttachAccountRequest request, CancellationToken ct = default);

    /// <summary>Aktualizuje dane per-org członka (DisplayName, Color, Priority, PhotoUrl).</summary>
    Task<MemberDetailResponse> UpdateAsync(Guid memberId, UpdateMemberRequest request, CancellationToken ct = default);

    /// <summary>Ustawia aktywność członkostwa (true/false).</summary>
    Task<MemberDetailResponse> SetActiveAsync(Guid memberId, bool isActive, CancellationToken ct = default);

    /// <summary>Dodaje rolę do członkostwa (np. Trainer).</summary>
    Task<MemberDetailResponse> AddRoleAsync(Guid memberId, MemberRole role, CancellationToken ct = default);

    /// <summary>Usuwa rolę z członkostwa. Nie można usunąć ostatniej roli Admin.</summary>
    Task<MemberDetailResponse> RemoveRoleAsync(Guid memberId, MemberRole role, CancellationToken ct = default);

    /// <summary>Przypisuje stałego trenera do uczestnika (relacja 1:N).</summary>
    Task AssignTrainerToParticipantAsync(Guid participantMemberId, Guid trainerMemberId, CancellationToken ct = default);

    /// <summary>Usuwa przypisanie trenera do uczestnika.</summary>
    Task RemoveTrainerFromParticipantAsync(Guid participantMemberId, Guid trainerMemberId, CancellationToken ct = default);

    /// <summary>Usuwa członkostwo (soft delete).</summary>
    Task DeleteAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>
    /// Wyszukuje osobę po kodzie profilu i sprawdza, czy jest już członkiem organizacji.
    /// Rzuca <see cref="ServiceException"/> z kodem <see cref="ServiceErrorCode.NotFound"/>
    /// gdy nie istnieje użytkownik z takim kodem.
    /// </summary>
    Task<MemberLookupResponse> FindByCodeAsync(Guid organizationId, string profileCode, CancellationToken ct = default);
}
