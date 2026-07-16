using BookingHub.Api.Dtos.Member;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie członkostwami w organizacji, rolami i przypisaniami trenerów.
///
/// Trasa bazowa: /api/organizations/{organizationId}/members
///
/// Odczyt list:
///   GET /                         — lista stronicowana (Admin, Manager, Trainer)
///   GET /all                      — pełna lista do selectów (Admin, Manager, Trainer)
///   GET /trainers                 — aktywni trenerzy (Admin, Manager, Trainer)
///   GET /participants             — aktywni uczestnicy (Admin, Manager, Trainer)
///   GET /{memberId}               — szczegóły (Admin, Manager lub właściciel)
///
/// Zarządzanie:
///   POST /add-existing            — dodaj istniejącą osobę (Admin, Manager)
///   POST /create-with-account     — utwórz konto Kinde + Person + Member (Admin)
///   POST /{memberId}/attach-account — przypisz konto do profilu bez konta (Admin)
///   PUT  /{memberId}              — edytuj dane per-org (Admin, Manager)
///   PATCH /{memberId}/active      — zmień aktywność (Admin)
///   POST  /{memberId}/roles       — dodaj rolę (Admin)
///   DELETE /{memberId}/roles/{role} — usuń rolę (Admin)
///   POST  /{memberId}/trainers    — przypisz trenera (Admin, Manager)
///   DELETE /{memberId}/trainers/{trainerId} — usuń trenera (Admin, Manager)
///   DELETE /{memberId}            — usuń członkostwo (Admin)
/// </summary>
[Route("api/organizations/{organizationId:guid}/members")]
[RequireOrgMembership]
public sealed class MembersController : BookingHubControllerBase
{
    private readonly IOrganizationMemberService _members;
    private readonly IRankService _rankService;

    public MembersController(IOrganizationMemberService members, IRankService rankService)
    {
        _members     = members;
        _rankService = rankService;
    }

    /// <summary>
    /// Stronicowana lista członków organizacji. Dostępna dla Admin, Manager i Trainer.
    /// </summary>
    [HttpGet]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(PagedResult<MemberSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MemberSummaryResponse>>> GetPaged(
        Guid organizationId,
        [FromQuery] OrganizationMemberFilterParams filter,
        CancellationToken ct)
    {
        var result = await _members.GetPagedAsync(organizationId, filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Pełna lista aktywnych członków (do selectów w formularzach).
    /// </summary>
    [HttpGet("all")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(IReadOnlyList<MemberSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MemberSummaryResponse>>> GetAll(
        Guid organizationId, CancellationToken ct)
    {
        var result = await _members.GetByOrganizationAsync(organizationId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lista aktywnych trenerów organizacji.
    /// </summary>
    [HttpGet("trainers")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(IReadOnlyList<MemberSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MemberSummaryResponse>>> GetTrainers(
        Guid organizationId, CancellationToken ct)
    {
        var result = await _members.GetTrainersAsync(organizationId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lista aktywnych uczestników organizacji.
    /// </summary>
    [HttpGet("participants")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager, OrgRoles.Trainer)]
    [ProducesResponseType(typeof(IReadOnlyList<MemberSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MemberSummaryResponse>>> GetParticipants(
        Guid organizationId, CancellationToken ct)
    {
        var result = await _members.GetParticipantsAsync(organizationId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Szczegóły członkostwa. Admin/Manager widzi każdego; Trainer i Participant tylko siebie.
    /// </summary>
    [HttpGet("{memberId:guid}")]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetailResponse>> GetById(
        Guid organizationId, Guid memberId, CancellationToken ct)
    {
        var member = await GetMemberInOrgOrThrowAsync(memberId, organizationId, ct);

        var isAdminOrManager = await CurrentUser.IsAdminOrManagerAsync(organizationId, ct);
        if (!isAdminOrManager)
        {
            var myMember = await CurrentUser.GetMemberAsync(organizationId, ct);
            if (myMember?.Id != memberId)
                throw new ServiceException(ServiceErrorCode.Forbidden,
                    "Możesz pobierać tylko własne dane członkostwa.");
        }

        return Ok(member);
    }

    /// <summary>
    /// Wyszukuje osobę po kodzie profilu i sprawdza, czy jest już członkiem organizacji.
    /// Celowo zwraca tylko imię/nazwisko — bez e-maila ani innych danych osobowych,
    /// aby zapobiec enumeracji kont (security by design).
    /// </summary>
    [HttpGet("find-by-code")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(MemberLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberLookupResponse>> FindByCode(
        Guid organizationId, [FromQuery] string code, CancellationToken ct)
    {
        var result = await _members.FindByCodeAsync(organizationId, code, ct);
        return Ok(result);
    }

    /// <summary>
    /// Dodaje istniejącą osobę jako nowego członka organizacji.
    /// </summary>
    [HttpPost("add-existing")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberDetailResponse>> AddExisting(
        Guid organizationId, [FromBody] AddMemberRequest request, CancellationToken ct)
    {
        var created = await _members.AddMemberAsync(organizationId, request, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, memberId = created.Id }, created);
    }

    /// <summary>
    /// Tworzy konto Kinde + Person + OrganizationMember w jednym kroku. Tylko Admin.
    /// </summary>
    [HttpPost("create-with-account")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberDetailResponse>> CreateWithAccount(
        Guid organizationId, [FromBody] CreateMemberWithAccountRequest request, CancellationToken ct)
    {
        var created = await _members.CreateMemberWithAccountAsync(organizationId, request, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, memberId = created.Id }, created);
    }

    /// <summary>
    /// Tworzy profil Person bez konta Kinde (np. dziecko) i dodaje do organizacji. Tylko Admin.
    /// </summary>
    [HttpPost("create-profile")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MemberDetailResponse>> CreateProfile(
        Guid organizationId, [FromBody] CreateMemberProfileRequest request, CancellationToken ct)
    {
        var created = await _members.CreateMemberProfileAsync(organizationId, request, ct);
        return CreatedAtAction(nameof(GetById),
            new { organizationId, memberId = created.Id }, created);
    }

    /// <summary>
    /// Przypisuje konto logowania (Kinde + User) do istniejącego profilu bez konta. Tylko Admin.
    /// Zwraca błąd 409 gdy email jest już zajęty lub profil już ma konto.
    /// </summary>
    [HttpPost("{memberId:guid}/attach-account")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberDetailResponse>> AttachAccount(
        Guid organizationId, Guid memberId,
        [FromBody] AttachAccountRequest request, CancellationToken ct)
    {
        var result = await _members.AttachAccountAsync(organizationId, memberId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Aktualizuje dane per-org członka (DisplayName, Color, Priority, PhotoUrl).
    /// Admin/Manager może edytować każdego; uczestnik tylko siebie.
    /// </summary>
    [HttpPut("{memberId:guid}")]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetailResponse>> Update(
        Guid organizationId, Guid memberId,
        [FromBody] UpdateMemberRequest request, CancellationToken ct)
    {
        await GetMemberInOrgOrThrowAsync(memberId, organizationId, ct);

        var isAdminOrManager = await CurrentUser.IsAdminOrManagerAsync(organizationId, ct);
        if (!isAdminOrManager)
        {
            var myMember = await CurrentUser.GetMemberAsync(organizationId, ct);
            if (myMember?.Id != memberId)
                throw new ServiceException(ServiceErrorCode.Forbidden,
                    "Możesz edytować tylko własne dane.");
        }

        var updated = await _members.UpdateAsync(memberId, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Zmienia aktywność członkostwa (aktywuje lub dezaktywuje). Tylko Admin.
    /// </summary>
    [HttpPatch("{memberId:guid}/active")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetailResponse>> SetActive(
        Guid organizationId, Guid memberId,
        [FromBody] SetMemberActiveRequest request, CancellationToken ct)
    {
        await GetMemberInOrgOrThrowAsync(memberId, organizationId, ct);
        var updated = await _members.SetActiveAsync(memberId, request.IsActive, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Dodaje rolę do członkostwa. Tylko Admin.
    /// </summary>
    [HttpPost("{memberId:guid}/roles")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberDetailResponse>> AddRole(
        Guid organizationId, Guid memberId,
        [FromBody] AddMemberRoleRequest request, CancellationToken ct)
    {
        await GetMemberInOrgOrThrowAsync(memberId, organizationId, ct);
        var updated = await _members.AddRoleAsync(memberId, request.Role, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Usuwa rolę z członkostwa. Tylko Admin. Nie można usunąć ostatniej roli Admin.
    /// </summary>
    [HttpDelete("{memberId:guid}/roles/{role}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberDetailResponse>> RemoveRole(
        Guid organizationId, Guid memberId, MemberRole role, CancellationToken ct)
    {
        await GetMemberInOrgOrThrowAsync(memberId, organizationId, ct);
        var updated = await _members.RemoveRoleAsync(memberId, role, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Przypisuje stałego trenera do uczestnika. Admin lub Manager.
    /// </summary>
    [HttpPost("{memberId:guid}/trainers")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignTrainer(
        Guid organizationId, Guid memberId,
        [FromBody] AssignTrainerToParticipantRequest request, CancellationToken ct)
    {
        await GetMemberInOrgOrThrowAsync(memberId, organizationId, ct);
        await GetMemberInOrgOrThrowAsync(request.TrainerMemberId, organizationId, ct);
        await _members.AssignTrainerToParticipantAsync(memberId, request.TrainerMemberId, ct);
        return NoContent();
    }

    /// <summary>
    /// Usuwa przypisanie trenera do uczestnika. Admin lub Manager.
    /// </summary>
    [HttpDelete("{memberId:guid}/trainers/{trainerId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTrainer(
        Guid organizationId, Guid memberId, Guid trainerId, CancellationToken ct)
    {
        await GetMemberInOrgOrThrowAsync(memberId, organizationId, ct);
        await _members.RemoveTrainerFromParticipantAsync(memberId, trainerId, ct);
        return NoContent();
    }

    /// <summary>
    /// Usuwa członkostwo (soft delete). Tylko Admin.
    /// </summary>
    [HttpDelete("{memberId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid organizationId, Guid memberId, CancellationToken ct)
    {
        await GetMemberInOrgOrThrowAsync(memberId, organizationId, ct);
        await _members.DeleteAsync(memberId, ct);
        return NoContent();
    }

    /// <summary>
    /// Ustawia lub usuwa rangę członka w ramach konkretnej dyscypliny. Tylko Admin.
    /// Wysłanie RankId = null usuwa przypisanie rangi w tej dyscyplinie.
    /// </summary>
    [HttpPut("{memberId:guid}/disciplines/{disciplineId:guid}/rank")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetailResponse>> SetRank(
        Guid organizationId, Guid memberId, Guid disciplineId,
        [FromBody] SetMemberRankRequest request, CancellationToken ct)
    {
        var updated = await _rankService.SetMemberRankAsync(organizationId, memberId, disciplineId, request.RankId, ct);
        return Ok(updated);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Pobiera członka i weryfikuje że należy do organizacji z trasy.
    /// Chroni przed IDOR — uniemożliwia operacje na członkach z innych organizacji.
    /// </summary>
    private async Task<MemberDetailResponse> GetMemberInOrgOrThrowAsync(
        Guid memberId, Guid organizationId, CancellationToken ct)
    {
        var member = await _members.GetByIdAsync(memberId, ct);
        if (member.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Członek {memberId} nie istnieje w tej organizacji.");
        return member;
    }
}
