using BookingHub.Api.Dtos.Cost;
using BookingHub.Api.Infrastructure.Authorization;
using BookingHub.Api.Infrastructure.Controllers;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.Api.Controllers;

/// <summary>
/// Zarządzanie stawkami (trenerów i grup) oraz kalkulacja rachunków.
///
/// Stawki trenerów:
///   GET    /api/organizations/{organizationId}/members/{memberId}/trainer-rates          — historia (Admin, Manager)
///   GET    /api/organizations/{organizationId}/members/{memberId}/trainer-rates/current  — aktualna (Admin, Manager)
///   POST   /api/organizations/{organizationId}/members/{memberId}/trainer-rates          — dodaj (Admin)
///   PATCH  /api/organizations/{organizationId}/members/{memberId}/trainer-rates/{rateId}/close — zamknij (Admin)
///   DELETE /api/organizations/{organizationId}/members/{memberId}/trainer-rates/{rateId} — usuń (Admin)
///
/// Rachunki:
///   GET /api/organizations/{organizationId}/billing/{year}/{month}                 — rachunek całej org (Admin, Manager)
///   GET /api/organizations/{organizationId}/members/{memberId}/billing/{year}/{month} — rachunek uczestnika (Admin, Manager lub właściciel)
/// </summary>
[Route("api/organizations/{organizationId:guid}")]
[RequireOrgMembership]
public sealed class CostsController : BookingHubControllerBase
{
    private readonly ICostService _costs;
    private readonly IOrganizationMemberRepository _members;

    public CostsController(ICostService costs, IOrganizationMemberRepository members)
    {
        _costs   = costs;
        _members = members;
    }

    // ── Stawki trenera ────────────────────────────────────────────────────────

    /// <summary>
    /// Historia stawek godzinowych trenera. Admin lub Manager.
    /// </summary>
    [HttpGet("members/{memberId:guid}/trainer-rates")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(IReadOnlyList<TrainerSessionRateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TrainerSessionRateResponse>>> GetTrainerRates(
        Guid organizationId, Guid memberId, CancellationToken ct)
    {
        await EnsureMemberInOrgAsync(memberId, organizationId, ct);
        var result = await _costs.GetTrainerRatesAsync(memberId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Aktualnie obowiązująca stawka godzinowa trenera. Admin lub Manager.
    /// </summary>
    [HttpGet("members/{memberId:guid}/trainer-rates/current")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(TrainerSessionRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TrainerSessionRateResponse?>> GetCurrentTrainerRate(
        Guid organizationId, Guid memberId, CancellationToken ct)
    {
        await EnsureMemberInOrgAsync(memberId, organizationId, ct);
        var rate = await _costs.GetCurrentTrainerRateAsync(memberId, ct);
        if (rate is null) return NoContent();
        return Ok(rate);
    }

    /// <summary>
    /// Dodaje nową stawkę godzinową trenera. Tylko Admin.
    /// Poprzednia aktywna stawka jest zamykana automatycznie.
    /// </summary>
    [HttpPost("members/{memberId:guid}/trainer-rates")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(TrainerSessionRateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrainerSessionRateResponse>> AddTrainerRate(
        Guid organizationId, Guid memberId,
        [FromBody] AddTrainerSessionRateRequest request, CancellationToken ct)
    {
        await EnsureMemberInOrgAsync(memberId, organizationId, ct);
        var rate = await _costs.AddTrainerRateAsync(memberId, request, ct);
        return StatusCode(StatusCodes.Status201Created, rate);
    }

    /// <summary>
    /// Zamknięcie aktywnej stawki trenera (ustawia ValidTo). Tylko Admin.
    /// </summary>
    [HttpPatch("members/{memberId:guid}/trainer-rates/{rateId:guid}/close")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(typeof(TrainerSessionRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainerSessionRateResponse>> CloseTrainerRate(
        Guid organizationId, Guid memberId, Guid rateId,
        [FromBody] CloseTrainerSessionRateRequest request, CancellationToken ct)
    {
        await EnsureMemberInOrgAsync(memberId, organizationId, ct);
        var rate = await _costs.CloseTrainerRateAsync(rateId, request, ct);
        return Ok(rate);
    }

    /// <summary>
    /// Usuwa stawkę trenera (tylko jeśli nie zamknięta). Tylko Admin.
    /// </summary>
    [HttpDelete("members/{memberId:guid}/trainer-rates/{rateId:guid}")]
    [RequireOrgMembership(OrgRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTrainerRate(
        Guid organizationId, Guid memberId, Guid rateId, CancellationToken ct)
    {
        await EnsureMemberInOrgAsync(memberId, organizationId, ct);
        await _costs.DeleteTrainerRateAsync(rateId, ct);
        return NoContent();
    }

    // ── Rachunki miesięczne ───────────────────────────────────────────────────

    /// <summary>
    /// Rachunek całej organizacji — lista rachunków wszystkich aktywnych uczestników.
    /// Admin lub Manager.
    /// </summary>
    [HttpGet("billing/{year:int}/{month:int}")]
    [RequireOrgMembership(OrgRoles.Admin, OrgRoles.Manager)]
    [ProducesResponseType(typeof(IReadOnlyList<MemberMonthlyBillResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MemberMonthlyBillResponse>>> GetOrganizationBilling(
        Guid organizationId, int year, int month, CancellationToken ct)
    {
        if (month < 1 || month > 12)
            throw new Services.Exceptions.ServiceException(Services.Exceptions.ServiceErrorCode.ValidationError,
                "Miesiąc musi być w zakresie 1–12.", "month");

        if (year < 2000 || year > 2100)
            throw new Services.Exceptions.ServiceException(Services.Exceptions.ServiceErrorCode.ValidationError,
                "Rok musi być w zakresie 2000–2100.", "year");

        var result = await _costs.CalculateOrganizationMonthlyBillAsync(organizationId, year, month, ct);
        return Ok(result);
    }

    /// <summary>
    /// Rachunek konkretnego uczestnika za podany miesiąc.
    /// Admin/Manager może sprawdzić każdego; uczestnik tylko siebie.
    /// Dostęp kontrolowany przez logikę metody (class-level [RequireOrgMembership] zapewnia min. członkostwo).
    /// </summary>
    [HttpGet("members/{memberId:guid}/billing/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(MemberMonthlyBillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberMonthlyBillResponse>> GetMemberBilling(
        Guid organizationId, Guid memberId, int year, int month, CancellationToken ct)
    {
        if (month < 1 || month > 12)
            throw new Services.Exceptions.ServiceException(Services.Exceptions.ServiceErrorCode.ValidationError,
                "Miesiąc musi być w zakresie 1–12.", "month");

        if (year < 2000 || year > 2100)
            throw new Services.Exceptions.ServiceException(Services.Exceptions.ServiceErrorCode.ValidationError,
                "Rok musi być w zakresie 2000–2100.", "year");

        var isAdminOrManager = await CurrentUser.IsAdminOrManagerAsync(organizationId, ct);
        if (!isAdminOrManager)
        {
            var myMember = await CurrentUser.GetMemberAsync(organizationId, ct);
            if (myMember?.Id != memberId)
                throw new Services.Exceptions.ServiceException(
                    Services.Exceptions.ServiceErrorCode.Forbidden,
                    "Możesz pobierać tylko własne rachunki.");
        }

        var result = await _costs.CalculateMemberMonthlyBillAsync(memberId, year, month, ct);
        return Ok(result);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task EnsureMemberInOrgAsync(Guid memberId, Guid organizationId, CancellationToken ct)
    {
        var member = await _members.GetByIdAsync(memberId, ct);
        if (member is null || member.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Członek {memberId} nie istnieje w tej organizacji.");
    }
}
