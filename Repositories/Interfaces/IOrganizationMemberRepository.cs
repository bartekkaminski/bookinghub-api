using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Repositories.Interfaces;

/// <summary>
/// Repozytorium członkostw w organizacjach (OrganizationMember).
/// </summary>
public interface IOrganizationMemberRepository : IBaseRepository<OrganizationMember>
{
    /// <summary>
    /// Pobiera członkostwo osoby w konkretnej organizacji.
    /// Zwraca null jeśli osoba nie jest członkiem tej organizacji.
    /// </summary>
    Task<OrganizationMember?> GetByPersonAndOrgAsync(Guid personId, Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera członkostwo z pełnymi danymi — Person (+ User), Organization, Roles.
    /// </summary>
    Task<OrganizationMember?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera członkostwo z rolami (Roles).
    /// </summary>
    Task<OrganizationMember?> GetWithRolesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera stronicowaną listę członków organizacji z opcjonalnym filtrowaniem.
    /// </summary>
    Task<PagedResult<OrganizationMember>> GetPagedAsync(OrganizationMemberFilterParams filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkich członków organizacji z daną rolą.
    /// </summary>
    Task<IReadOnlyList<OrganizationMember>> GetByRoleAsync(Guid organizationId, MemberRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkich trenerów w organizacji (członkowie z rolą Trainer).
    /// </summary>
    Task<IReadOnlyList<OrganizationMember>> GetTrainersAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkich uczestników/zawodników w organizacji (członkowie z rolą Participant).
    /// </summary>
    Task<IReadOnlyList<OrganizationMember>> GetParticipantsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy osoba jest już członkiem organizacji.
    /// </summary>
    Task<bool> IsMemberAsync(Guid personId, Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ustawia flagę IsActive dla członkostwa. Zwraca false jeśli nie istnieje.
    /// </summary>
    Task<bool> SetActiveAsync(Guid memberId, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkich stałych trenerów przypisanych do uczestnika.
    /// </summary>
    Task<IReadOnlyList<OrganizationMember>> GetAssignedTrainersAsync(Guid participantMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkich uczestników przypisanych na stałe do trenera.
    /// </summary>
    Task<IReadOnlyList<OrganizationMember>> GetAssignedParticipantsAsync(Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkich członków przypisanych do grupy zajęciowej.
    /// </summary>
    Task<IReadOnlyList<OrganizationMember>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkich członków przypisanych do zespołu.
    /// </summary>
    Task<IReadOnlyList<OrganizationMember>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkich aktywnych członków organizacji (do selectów / list).
    /// </summary>
    Task<IReadOnlyList<OrganizationMember>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie członkostwa danej osoby (we wszystkich organizacjach).
    /// </summary>
    Task<IReadOnlyList<OrganizationMember>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca liczbę aktywnych członków w organizacji.
    /// </summary>
    Task<int> CountByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy organizacja ma co najmniej jednego aktywnego członka.
    /// </summary>
    Task<bool> AnyActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca liczbę aktywnych Adminów w organizacji.
    /// </summary>
    Task<int> CountAdminsInOrgAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tworzy relację ParticipantTrainer (przypisanie trenera do uczestnika).
    /// </summary>
    Task AddParticipantTrainerAsync(Guid participantMemberId, Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa relację ParticipantTrainer.
    /// </summary>
    Task RemoveParticipantTrainerAsync(Guid participantMemberId, Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza, czy relacja ParticipantTrainer istnieje.
    /// </summary>
    Task<bool> ParticipantTrainerExistsAsync(Guid participantMemberId, Guid trainerMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bezpośrednio dodaje rolę do członkostwa (omija UpdateAsync, który nie zapisuje nawigacji).
    /// </summary>
    Task AddRoleDirectAsync(Guid memberId, MemberRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bezpośrednio usuwa rolę z członkostwa.
    /// </summary>
    Task RemoveRoleDirectAsync(Guid memberId, MemberRole role, CancellationToken cancellationToken = default);
}
