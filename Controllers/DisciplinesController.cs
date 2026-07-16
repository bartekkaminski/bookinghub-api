using BookingHub.Api.Dtos.Discipline;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie dyscyplinami organizacyjnymi (grupują rangi w niezależne "tory" awansu,
/// np. "Latino", "Standard" w szkole tańca albo "Pasy" w klubie karate).
///
/// Trasa bazowa: /api/organizations/{organizationId}/disciplines
///
/// Odczyt:
///   GET /                   — lista dyscyplin z liczbą rang (wszyscy członkowie)
///   GET /{disciplineId}     — szczegóły dyscypliny (wszyscy członkowie)
///
/// Zarządzanie (tylko Admin):
///   POST /                  — utwórz dyscyplinę
///   PUT  /{disciplineId}    — zaktualizuj dyscyplinę
///   DELETE /{disciplineId}  — usuń dyscyplinę (soft delete, blokowane jeśli ma rangi)
/// </summary>
[Route("api/organizations/{organizationId:guid}/disciplines")]
[RequireOrgMembership]
public sealed class DisciplinesController : BookingHubControllerBase
{
    private readonly IDisciplineService _disciplineService;

    public DisciplinesController(IDisciplineService disciplineService)
    {
        _disciplineService = disciplineService;
    }

    /// <summary>
    /// Pobiera wszystkie dyscypliny organizacji z liczbą zdefiniowanych rang.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DisciplineSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DisciplineSummaryResponse>>> GetAll(
        Guid organizationId, CancellationToken ct)
    {
        var result = await _disciplineService.GetAllAsync(organizationId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Pobiera szczegóły dyscypliny.
    /// </summary>
    [HttpGet("{disciplineId:guid}")]
    [ProducesResponseType(typeof(DisciplineDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DisciplineDetailResponse>> GetById(
        Guid organizationId, Guid disciplineId, CancellationToken ct)
    {
        var result = await _disciplineService.GetByIdAsync(organizationId, disciplineId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Tworzy nową dyscyplinę w organizacji. Tylko Admin.
    /// </summary>
    [HttpPost]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(DisciplineDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DisciplineDetailResponse>> Create(
        Guid organizationId, [FromBody] CreateDisciplineRequest request, CancellationToken ct)
    {
        var created = await _disciplineService.CreateAsync(organizationId, request, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, disciplineId = created.Id }, created);
    }

    /// <summary>
    /// Aktualizuje dane dyscypliny (nazwa, kolor). Tylko Admin.
    /// </summary>
    [HttpPut("{disciplineId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(DisciplineDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DisciplineDetailResponse>> Update(
        Guid organizationId, Guid disciplineId,
        [FromBody] UpdateDisciplineRequest request, CancellationToken ct)
    {
        var updated = await _disciplineService.UpdateAsync(organizationId, disciplineId, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Usuwa dyscyplinę (soft delete). Wymaga braku rang w dyscyplinie. Tylko Admin.
    /// </summary>
    [HttpDelete("{disciplineId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid organizationId, Guid disciplineId, CancellationToken ct)
    {
        await _disciplineService.DeleteAsync(organizationId, disciplineId, ct);
        return NoContent();
    }
}
