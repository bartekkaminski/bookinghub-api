using BookingHub.Api.Dtos.Organization;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie organizacjami.
///
/// GET    /api/organizations                  — lista własnych organizacji (zalogowany)
/// GET    /api/organizations/{organizationId} — szczegóły (tylko członek)
/// POST   /api/organizations                  — utwórz (zalogowany → twórca Admin)
/// PUT    /api/organizations/{organizationId} — edytuj (Admin lub Manager)
/// DELETE /api/organizations/{organizationId} — usuń (tylko Admin)
/// </summary>
[Route("api/organizations")]
public sealed class OrganizationsController : BookingHubControllerBase
{
    private readonly IOrganizationService _organizations;

    public OrganizationsController(IOrganizationService organizations)
    {
        _organizations = organizations;
    }

    /// <summary>
    /// Stronicowana lista organizacji zalogowanego użytkownika (tylko te, do których należy).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrganizationSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrganizationSummaryResponse>>> GetPaged(
        [FromQuery] OrganizationFilterParams filter, CancellationToken ct)
    {
        // Filtruj po zalogowanym — każdy widzi tylko własne organizacje.
        var personId = CurrentUser.PersonId;
        var result = await _organizations.GetPagedAsync(filter, personId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Szczegóły organizacji wraz z licznikami członków, grup i zespołów.
    /// Dostępne tylko dla aktywnych członków tej organizacji.
    /// </summary>
    [HttpGet("{organizationId:guid}")]
    [RequireOrgMembership]
    [ProducesResponseType(typeof(OrganizationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganizationDetailResponse>> GetById(Guid organizationId, CancellationToken ct)
    {
        var org = await _organizations.GetByIdAsync(organizationId, ct);
        return Ok(org);
    }

    /// <summary>
    /// Tworzy nową organizację. Wywołujący automatycznie staje się pierwszym Adminem.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrganizationDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrganizationDetailResponse>> Create(
        [FromBody] CreateOrganizationRequest request, CancellationToken ct)
    {
        var creatorPersonId = RequirePersonId();
        var created = await _organizations.CreateAsync(request, creatorPersonId, ct);
        return CreatedAtAction(nameof(GetById), new { organizationId = created.Id }, created);
    }

    /// <summary>
    /// Aktualizuje dane organizacji. Wymaga roli Admin lub Manager.
    /// </summary>
    [HttpPut("{organizationId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(OrganizationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrganizationDetailResponse>> Update(
        Guid organizationId, [FromBody] UpdateOrganizationRequest request, CancellationToken ct)
    {
        var updated = await _organizations.UpdateAsync(organizationId, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Usuwa organizację (soft delete). Tylko Admin. Wymaga braku aktywnych członkostw.
    /// </summary>
    [HttpDelete("{organizationId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid organizationId, CancellationToken ct)
    {
        await _organizations.DeleteAsync(organizationId, ct);
        return NoContent();
    }
}
