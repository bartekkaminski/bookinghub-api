using BookingHub.Api.Dtos.Organization;
using BookingHub.Api.Repositories.Common;

namespace BookingHub.Api.Services.Interfaces;

/// <summary>
/// Serwis zarządzania organizacjami.
/// </summary>
public interface IOrganizationService
{
    /// <summary>Pobiera stronicowaną listę organizacji — tylko te, do których należy dany użytkownik.</summary>
    Task<PagedResult<OrganizationSummaryResponse>> GetPagedAsync(OrganizationFilterParams filter, Guid? personId = null, CancellationToken ct = default);

    /// <summary>Pobiera szczegóły organizacji wraz z licznikami.</summary>
    Task<OrganizationDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Tworzy nową organizację i automatycznie dodaje twórcę jako pierwszego Admina.
    /// </summary>
    /// <param name="request">Dane organizacji.</param>
    /// <param name="creatorPersonId">PersonId zalogowanego użytkownika — zostanie Adminem.</param>
    Task<OrganizationDetailResponse> CreateAsync(CreateOrganizationRequest request, Guid creatorPersonId, CancellationToken ct = default);

    /// <summary>Aktualizuje dane organizacji.</summary>
    Task<OrganizationDetailResponse> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default);

    /// <summary>Usuwa organizację (soft delete). Tylko gdy brak aktywnych członkostw.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Zwraca informację o limicie tworzenia organizacji i aktualnym stanie dla danego użytkownika.
    /// </summary>
    Task<OrganizationCreationLimitsResponse> GetCreationLimitsAsync(Guid personId, CancellationToken ct = default);
}
