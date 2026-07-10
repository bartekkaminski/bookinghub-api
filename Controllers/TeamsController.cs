using BookingHub.Api.Dtos.Team;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie zespołami i ich składem.
///
/// Trasa bazowa: /api/organizations/{organizationId}/teams
///
/// Odczyt:
///   GET /            — lista stronicowana (Admin, Manager, Trainer)
///   GET /all         — pełna lista do selectów (Admin, Manager, Trainer)
///   GET /{teamId}    — szczegóły (Admin, Manager, Trainer)
///
/// Zarządzanie:
///   POST /           — utwórz (Admin, Manager)
///   PUT  /{teamId}   — edytuj (Admin, Manager)
///   DELETE /{teamId} — usuń (Admin)
///
/// Skład:
///   POST   /{teamId}/members                      — dodaj uczestnika (Admin, Manager)
///   DELETE /{teamId}/members/{organizationMemberId} — usuń uczestnika (Admin, Manager)
///   POST   /{teamId}/trainers                     — przypisz trenera (Admin, Manager)
///   DELETE /{teamId}/trainers/{trainerId}         — usuń trenera (Admin, Manager)
/// </summary>
[Route("api/organizations/{organizationId:guid}/teams")]
[RequireOrgMembership]
public sealed class TeamsController : BookingHubControllerBase
{
    private readonly ITeamService _teams;

    public TeamsController(ITeamService teams)
    {
        _teams = teams;
    }

    /// <summary>
    /// Stronicowana lista zespołów w organizacji.
    /// </summary>
    [HttpGet]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(PagedResult<TeamSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TeamSummaryResponse>>> GetPaged(
        Guid organizationId, [FromQuery] TeamFilterParams filter, CancellationToken ct)
    {
        var result = await _teams.GetPagedAsync(organizationId, filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Pełna lista aktywnych zespołów (do selectów w formularzach).
    /// </summary>
    [HttpGet("all")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(IReadOnlyList<TeamSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeamSummaryResponse>>> GetAll(
        Guid organizationId, CancellationToken ct)
    {
        var result = await _teams.GetByOrganizationAsync(organizationId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Szczegóły zespołu: skład, trenerzy, grupy.
    /// </summary>
    [HttpGet("{teamId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(TeamDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamDetailResponse>> GetById(
        Guid organizationId, Guid teamId, CancellationToken ct)
    {
        var team = await _teams.GetByIdAsync(teamId, ct);
        if (team.OrganizationId != organizationId)
            throw new Services.Exceptions.ServiceException(Services.Exceptions.ServiceErrorCode.NotFound,
                $"Zespół {teamId} nie istnieje w tej organizacji.");
        return Ok(team);
    }

    /// <summary>
    /// Tworzy nowy zespół. Admin lub Manager.
    /// </summary>
    [HttpPost]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(TeamDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeamDetailResponse>> Create(
        Guid organizationId, [FromBody] CreateTeamRequest request, CancellationToken ct)
    {
        var created = await _teams.CreateAsync(organizationId, request, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, teamId = created.Id }, created);
    }

    /// <summary>
    /// Aktualizuje dane zespołu. Admin lub Manager.
    /// </summary>
    [HttpPut("{teamId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(TeamDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamDetailResponse>> Update(
        Guid organizationId, Guid teamId,
        [FromBody] UpdateTeamRequest request, CancellationToken ct)
    {
        var updated = await _teams.UpdateAsync(teamId, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Usuwa zespół (soft delete). Tylko Admin.
    /// </summary>
    [HttpDelete("{teamId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid organizationId, Guid teamId, CancellationToken ct)
    {
        await _teams.DeleteAsync(teamId, ct);
        return NoContent();
    }

    // ── Skład zespołu ─────────────────────────────────────────────────────────

    /// <summary>
    /// Dodaje uczestnika do zespołu. Admin lub Manager.
    /// </summary>
    [HttpPost("{teamId:guid}/members")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddMember(
        Guid organizationId, Guid teamId,
        [FromBody] AddMemberToTeamRequest request, CancellationToken ct)
    {
        await _teams.AddMemberAsync(teamId, request.OrganizationMemberId, ct);
        return NoContent();
    }

    /// <summary>
    /// Usuwa uczestnika z zespołu. Admin lub Manager.
    /// </summary>
    [HttpDelete("{teamId:guid}/members/{organizationMemberId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(
        Guid organizationId, Guid teamId, Guid organizationMemberId, CancellationToken ct)
    {
        await _teams.RemoveMemberAsync(teamId, organizationMemberId, ct);
        return NoContent();
    }

    /// <summary>
    /// Przypisuje stałego trenera do zespołu. Admin lub Manager.
    /// </summary>
    [HttpPost("{teamId:guid}/trainers")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignTrainer(
        Guid organizationId, Guid teamId,
        [FromBody] AssignTrainerToTeamRequest request, CancellationToken ct)
    {
        await _teams.AssignTrainerAsync(teamId, request.TrainerMemberId, ct);
        return NoContent();
    }

    /// <summary>
    /// Usuwa przypisanie trenera do zespołu. Admin lub Manager.
    /// </summary>
    [HttpDelete("{teamId:guid}/trainers/{trainerId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTrainer(
        Guid organizationId, Guid teamId, Guid trainerId, CancellationToken ct)
    {
        await _teams.RemoveTrainerAsync(teamId, trainerId, ct);
        return NoContent();
    }
}
