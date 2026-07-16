using BookingHub.Api.Dtos.Cost;
using BookingHub.Api.Dtos.Group;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie grupami zajęciowymi, ich składem i stawkami.
///
/// Trasa bazowa: /api/organizations/{organizationId}/groups
///
/// Odczyt:
///   GET /              — lista stronicowana (Admin, Manager, Trainer)
///   GET /all           — pełna lista do selectów (Admin, Manager, Trainer)
///   GET /{groupId}     — szczegóły (Admin, Manager, Trainer)
///
/// Zarządzanie:
///   POST /             — utwórz (Admin, Manager)
///   PUT  /{groupId}    — edytuj (Admin, Manager)
///   DELETE /{groupId}  — usuń (Admin)
///
/// Skład grupy:
///   POST   /{groupId}/members                      — dodaj uczestnika (Admin, Manager)
///   DELETE /{groupId}/members/{organizationMemberId} — usuń uczestnika (Admin, Manager)
///   POST   /{groupId}/teams                        — dodaj zespół (Admin, Manager)
///   DELETE /{groupId}/teams/{teamId}               — usuń zespół (Admin, Manager)
///   POST   /{groupId}/trainers                     — przypisz trenera (Admin, Manager)
///   DELETE /{groupId}/trainers/{trainerId}         — usuń trenera (Admin, Manager)
///
/// Stawki:
///   GET    /{groupId}/cost-rates                   — historia stawek (Admin, Manager)
///   POST   /{groupId}/cost-rates                   — dodaj stawkę (Admin)
///   PATCH  /{groupId}/cost-rates/{rateId}/close    — zamknij stawkę (Admin)
///   DELETE /{groupId}/cost-rates/{rateId}          — usuń stawkę (Admin)
/// </summary>
[Route("api/organizations/{organizationId:guid}/groups")]
[RequireOrgMembership]
public sealed class GroupsController : BookingHubControllerBase
{
    private readonly IGroupService _groups;

    public GroupsController(IGroupService groups)
    {
        _groups = groups;
    }

    /// <summary>
    /// Stronicowana lista grup w organizacji.
    /// Trenerzy widzą wyłącznie grupy, do których są przypisani — filtr jest wymuszany
    /// po stronie serwera niezależnie od parametrów żądania.
    /// </summary>
    [HttpGet]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(PagedResult<GroupSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<GroupSummaryResponse>>> GetPaged(
        Guid organizationId, [FromQuery] GroupFilterParams filter, CancellationToken ct)
    {
        var isAdminOrManager = await CurrentUser.IsAdminOrManagerAsync(organizationId, ct);
        if (!isAdminOrManager)
        {
            var myMember = await CurrentUser.GetMemberAsync(organizationId, ct);
            filter.TrainerMemberId = myMember?.Id;
        }

        var result = await _groups.GetPagedAsync(organizationId, filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Pełna lista aktywnych grup (do selectów w formularzach).
    /// Trenerzy widzą wyłącznie własne grupy — filtr wymuszany po stronie serwera.
    /// </summary>
    [HttpGet("all")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(IReadOnlyList<GroupSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupSummaryResponse>>> GetAll(
        Guid organizationId, CancellationToken ct)
    {
        var isAdminOrManager = await CurrentUser.IsAdminOrManagerAsync(organizationId, ct);
        if (!isAdminOrManager)
        {
            var myMember = await CurrentUser.GetMemberAsync(organizationId, ct);
            if (myMember is null) return Ok(Array.Empty<GroupSummaryResponse>());
            var trainerGroups = await _groups.GetByTrainerAsync(myMember.Id, ct);
            return Ok(trainerGroups);
        }

        var result = await _groups.GetByOrganizationAsync(organizationId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Szczegóły grupy: skład, zespoły, historia stawek.
    /// </summary>
    [HttpGet("{groupId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(GroupDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDetailResponse>> GetById(
        Guid organizationId, Guid groupId, CancellationToken ct)
    {
        var group = await _groups.GetByIdAsync(groupId, ct);
        if (group.OrganizationId != organizationId)
            throw new Services.Exceptions.ServiceException(Services.Exceptions.ServiceErrorCode.NotFound,
                $"Grupa {groupId} nie istnieje w tej organizacji.");
        return Ok(group);
    }

    /// <summary>
    /// Tworzy nową grupę zajęciową. Admin lub Manager.
    /// </summary>
    [HttpPost]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(GroupDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GroupDetailResponse>> Create(
        Guid organizationId, [FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        var created = await _groups.CreateAsync(organizationId, request, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, groupId = created.Id }, created);
    }

    /// <summary>
    /// Aktualizuje dane grupy (nazwa, opis, kolor, aktywność). Admin lub Manager.
    /// </summary>
    [HttpPut("{groupId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(GroupDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GroupDetailResponse>> Update(
        Guid organizationId, Guid groupId,
        [FromBody] UpdateGroupRequest request, CancellationToken ct)
    {
        var updated = await _groups.UpdateAsync(groupId, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Usuwa grupę (soft delete). Tylko Admin. Wymaga braku aktywnych zajęć.
    /// </summary>
    [HttpDelete("{groupId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid organizationId, Guid groupId, CancellationToken ct)
    {
        await _groups.DeleteAsync(groupId, ct);
        return NoContent();
    }

    // ── Skład grupy ────────────────────────────────────────────────────────────

    /// <summary>
    /// Dodaje uczestnika do grupy. Admin lub Manager.
    /// </summary>
    [HttpPost("{groupId:guid}/members")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddMember(
        Guid organizationId, Guid groupId,
        [FromBody] AddMemberToGroupRequest request, CancellationToken ct)
    {
        await _groups.AddMemberAsync(groupId, request.OrganizationMemberId, ct);
        return NoContent();
    }

    /// <summary>
    /// Usuwa uczestnika z grupy. Admin lub Manager.
    /// </summary>
    [HttpDelete("{groupId:guid}/members/{organizationMemberId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(
        Guid organizationId, Guid groupId, Guid organizationMemberId, CancellationToken ct)
    {
        await _groups.RemoveMemberAsync(groupId, organizationMemberId, ct);
        return NoContent();
    }

    /// <summary>
    /// Dodaje zespół do grupy. Admin lub Manager.
    /// </summary>
    [HttpPost("{groupId:guid}/teams")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddTeam(
        Guid organizationId, Guid groupId,
        [FromBody] AddTeamToGroupRequest request, CancellationToken ct)
    {
        await _groups.AddTeamAsync(groupId, request.TeamId, ct);
        return NoContent();
    }

    /// <summary>
    /// Usuwa zespół z grupy. Admin lub Manager.
    /// </summary>
    [HttpDelete("{groupId:guid}/teams/{teamId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTeam(
        Guid organizationId, Guid groupId, Guid teamId, CancellationToken ct)
    {
        await _groups.RemoveTeamAsync(groupId, teamId, ct);
        return NoContent();
    }

    /// <summary>
    /// Przypisuje stałego trenera do grupy. Admin lub Manager.
    /// </summary>
    [HttpPost("{groupId:guid}/trainers")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignTrainer(
        Guid organizationId, Guid groupId,
        [FromBody] AssignTrainerToGroupRequest request, CancellationToken ct)
    {
        await _groups.AssignTrainerAsync(groupId, request.TrainerMemberId, ct);
        return NoContent();
    }

    /// <summary>
    /// Usuwa przypisanie trenera do grupy. Admin lub Manager.
    /// </summary>
    [HttpDelete("{groupId:guid}/trainers/{trainerId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTrainer(
        Guid organizationId, Guid groupId, Guid trainerId, CancellationToken ct)
    {
        await _groups.RemoveTrainerAsync(groupId, trainerId, ct);
        return NoContent();
    }

    // ── Stawki grupy ─────────────────────────────────────────────────────────

    /// <summary>
    /// Historia stawek miesięcznych grupy. Admin lub Manager.
    /// </summary>
    [HttpGet("{groupId:guid}/cost-rates")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(IReadOnlyList<GroupCostRateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<GroupCostRateResponse>>> GetCostRates(
        Guid organizationId, Guid groupId, CancellationToken ct)
    {
        var rates = await _groups.GetCostRatesAsync(groupId, ct);
        return Ok(rates);
    }

    /// <summary>
    /// Dodaje nową stawkę miesięczną. Tylko Admin.
    /// Poprzednia aktywna stawka jest zamykana automatycznie.
    /// </summary>
    [HttpPost("{groupId:guid}/cost-rates")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(GroupCostRateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GroupCostRateResponse>> AddCostRate(
        Guid organizationId, Guid groupId,
        [FromBody] AddGroupCostRateRequest request, CancellationToken ct)
    {
        var rate = await _groups.AddCostRateAsync(groupId, request, ct);
        return StatusCode(StatusCodes.Status201Created, rate);
    }

    /// <summary>
    /// Zamknięcie aktywnej stawki (ustawia ValidTo). Tylko Admin.
    /// </summary>
    [HttpPatch("{groupId:guid}/cost-rates/{rateId:guid}/close")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(GroupCostRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupCostRateResponse>> CloseCostRate(
        Guid organizationId, Guid groupId, Guid rateId,
        [FromBody] CloseGroupCostRateRequest request, CancellationToken ct)
    {
        var rate = await _groups.CloseCostRateAsync(rateId, request, ct);
        return Ok(rate);
    }

    /// <summary>
    /// Usuwa stawkę (tylko jeśli nie była używana do rozliczeń). Tylko Admin.
    /// </summary>
    [HttpDelete("{groupId:guid}/cost-rates/{rateId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCostRate(
        Guid organizationId, Guid groupId, Guid rateId, CancellationToken ct)
    {
        await _groups.DeleteCostRateAsync(rateId, ct);
        return NoContent();
    }
}
