using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Interfaces;
using System.Security.Claims;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis aktualnie zalogowanego użytkownika.
/// Encja User jest ładowana przez middleware i przechowywana w HttpContext.Items.
/// Wywołania GetMemberAsync/HasRoleAsync są leniwe i cachowane per żądanie.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOrganizationMemberRepository _memberRepo;

    /// <summary>Klucz pod którym middleware przechowuje encję User w HttpContext.Items.</summary>
    public const string CurrentUserKey = "CurrentUser";

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IOrganizationMemberRepository memberRepo)
    {
        _httpContextAccessor = httpContextAccessor;
        _memberRepo          = memberRepo;
    }

    /// <inheritdoc/>
    public string? ExternalId =>
        _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
        ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <inheritdoc/>
    public Guid? UserId => CurrentUser?.Id;

    /// <inheritdoc/>
    public Guid? PersonId => CurrentUser?.Person?.Id;

    /// <inheritdoc/>
    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true
        && CurrentUser?.IsActive == true;

    /// <inheritdoc/>
    public User? CurrentUser =>
        _httpContextAccessor.HttpContext?.Items[CurrentUserKey] as User;

    // ── Per-request cache dla ról ─────────────────────────────────────────────

    private readonly Dictionary<Guid, OrganizationMember?> _memberCache = [];

    /// <inheritdoc/>
    public async Task<OrganizationMember?> GetMemberAsync(Guid organizationId, CancellationToken ct = default)
    {
        if (PersonId is null)
            return null;

        if (_memberCache.TryGetValue(organizationId, out var cached))
            return cached;

        var member = await _memberRepo.GetByPersonAndOrgAsync(PersonId.Value, organizationId, ct);
        _memberCache[organizationId] = member;
        return member;
    }

    /// <inheritdoc/>
    public async Task<bool> HasRoleAsync(Guid organizationId, MemberRole role, CancellationToken ct = default)
    {
        var member = await GetMemberAsync(organizationId, ct);
        return member is { IsActive: true }
            && member.Roles.Any(r => r.Role == role);
    }

    /// <inheritdoc/>
    public async Task<bool> IsAdminAsync(Guid organizationId, CancellationToken ct = default)
        => await HasRoleAsync(organizationId, MemberRole.Admin, ct);

    /// <inheritdoc/>
    public async Task<bool> IsManagerAsync(Guid organizationId, CancellationToken ct = default)
        => await HasRoleAsync(organizationId, MemberRole.Manager, ct);

    /// <inheritdoc/>
    public async Task<bool> IsTrainerAsync(Guid organizationId, CancellationToken ct = default)
        => await HasRoleAsync(organizationId, MemberRole.Trainer, ct);

    /// <inheritdoc/>
    public async Task<bool> IsAdminOrManagerAsync(Guid organizationId, CancellationToken ct = default)
    {
        var member = await GetMemberAsync(organizationId, ct);
        return member is { IsActive: true }
            && member.Roles.Any(r => r.Role == MemberRole.Admin || r.Role == MemberRole.Manager);
    }
}
