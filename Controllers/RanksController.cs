using BookingHub.Api.Dtos.Member;
using BookingHub.Api.Dtos.Rank;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie rangami w ramach dyscypliny organizacyjnej.
///
/// Trasa bazowa: /api/organizations/{organizationId}/disciplines/{disciplineId}/ranks
///
/// Odczyt:
///   GET /                   — lista rang z liczbą członków (wszyscy członkowie)
///   GET /{rankId}           — szczegóły rangi (wszyscy członkowie)
///   GET /{rankId}/members   — stronicowana lista członków z rangą (wszyscy członkowie)
///
/// Zarządzanie (tylko Admin):
///   POST /                  — utwórz rangę
///   PUT  /{rankId}          — zaktualizuj rangę
///   DELETE /{rankId}        — usuń rangę (soft delete, usuwa przypisania u członków)
/// </summary>
[Route("api/organizations/{organizationId:guid}/disciplines/{disciplineId:guid}/ranks")]
[RequireOrgMembership]
public sealed class RanksController : BookingHubControllerBase
{
    private readonly IRankService _rankService;

    public RanksController(IRankService rankService)
    {
        _rankService = rankService;
    }

    /// <summary>
    /// Pobiera wszystkie rangi dyscypliny z liczbą przypisanych członków.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RankSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RankSummaryResponse>>> GetAll(
        Guid organizationId, Guid disciplineId, CancellationToken ct)
    {
        var result = await _rankService.GetAllAsync(organizationId, disciplineId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Pobiera szczegóły rangi.
    /// </summary>
    [HttpGet("{rankId:guid}")]
    [ProducesResponseType(typeof(RankDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RankDetailResponse>> GetById(
        Guid organizationId, Guid disciplineId, Guid rankId, CancellationToken ct)
    {
        var result = await _rankService.GetByIdAsync(organizationId, disciplineId, rankId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Pobiera stronicowaną listę członków posiadających tę rangę.
    /// </summary>
    [HttpGet("{rankId:guid}/members")]
    [ProducesResponseType(typeof(PagedResult<MemberSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<MemberSummaryResponse>>> GetMembers(
        Guid organizationId,
        Guid disciplineId,
        Guid rankId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _rankService.GetMembersAsync(organizationId, disciplineId, rankId, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Tworzy nową rangę w dyscyplinie. Tylko Admin.
    /// </summary>
    [HttpPost]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(RankDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RankDetailResponse>> Create(
        Guid organizationId, Guid disciplineId, [FromBody] CreateRankRequest request, CancellationToken ct)
    {
        var created = await _rankService.CreateAsync(organizationId, disciplineId, request, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, disciplineId, rankId = created.Id }, created);
    }

    /// <summary>
    /// Aktualizuje dane rangi (nazwa, kolor). Tylko Admin.
    /// </summary>
    [HttpPut("{rankId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(RankDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RankDetailResponse>> Update(
        Guid organizationId, Guid disciplineId, Guid rankId,
        [FromBody] UpdateRankRequest request, CancellationToken ct)
    {
        var updated = await _rankService.UpdateAsync(organizationId, disciplineId, rankId, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Usuwa rangę (soft delete). Członkowie tracą przypisanie. Tylko Admin.
    /// </summary>
    [HttpDelete("{rankId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid organizationId, Guid disciplineId, Guid rankId, CancellationToken ct)
    {
        await _rankService.DeleteAsync(organizationId, disciplineId, rankId, ct);
        return NoContent();
    }
}
