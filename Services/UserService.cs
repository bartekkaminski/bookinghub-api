using BookingHub.Api.Dtos.Auth;
using BookingHub.Api.Dtos.User;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania kontami logowania (User).
/// Odpowiada za provisioning przy pierwszym logowaniu i zarządzanie stanem konta.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IPersonRepository _persons;
    private readonly IOrganizationMemberRepository _members;
    private readonly IKindeManagementService _kinde;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository users,
        IPersonRepository persons,
        IOrganizationMemberRepository members,
        IKindeManagementService kinde,
        ILogger<UserService> logger)
    {
        _users   = users;
        _persons = persons;
        _members = members;
        _kinde   = kinde;
        _logger  = logger;
    }

    /// <inheritdoc/>
    public async Task<AuthMeResponse> ProvisionAsync(ProvisionUserRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalId))
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "ExternalId jest wymagany.", nameof(request.ExternalId));

        if (string.IsNullOrWhiteSpace(request.AuthProvider))
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "AuthProvider jest wymagany.", nameof(request.AuthProvider));

        // Pobierz lub utwórz User
        var user = await _users.GetByExternalIdAsync(request.ExternalId, request.AuthProvider, ct);
        if (user is null)
        {
            var profileCode = await GenerateUniqueProfileCodeAsync(ct);
            user = new User
            {
                ExternalId   = request.ExternalId,
                AuthProvider = request.AuthProvider,
                Email        = request.Email?.Trim() ?? string.Empty,
                IsActive     = true,
                ProfileCode  = profileCode,
            };
            user = await _users.AddAsync(user, ct);
            _logger.LogInformation("Provisioned nowego użytkownika {UserId} dla ExternalId {ExternalId}.",
                user.Id, request.ExternalId);
        }
        else if (string.IsNullOrEmpty(user.ProfileCode))
        {
            // Backfill: nadaj kod istniejącym użytkownikom bez kodu (np. stworzonym przed tą migracją)
            user.ProfileCode = await GenerateUniqueProfileCodeAsync(ct);
            await _users.UpdateAsync(user, ct);
        }
        else
        {
            // Synchronizuj e-mail jeśli się zmienił
            if (!string.IsNullOrWhiteSpace(request.Email) &&
                !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = request.Email.Trim();
                await _users.UpdateAsync(user, ct);
            }
        }

        // Upewnij się, że Person istnieje (lazy create przy braku profilu)
        var person = await _persons.GetByUserIdAsync(user.Id, ct);
        if (person is null)
        {
            person = new Person
            {
                UserId    = user.Id,
                FirstName = request.FirstName?.Trim(),
                LastName  = request.LastName?.Trim(),
            };
            person = await _persons.AddAsync(person, ct);
        }

        // Zbierz członkostwa z rolami dla AuthMeResponse
        var memberships = await _members.GetByPersonIdAsync(person.Id, ct);
        var membershipInfos = memberships.Select(m => new AuthMembershipInfo
        {
            MemberId         = m.Id,
            OrganizationId   = m.OrganizationId,
            OrganizationName = m.Organization?.Name ?? string.Empty,
            IsActive         = m.IsActive,
            Roles            = m.Roles.Select(r => r.Role.ToString()).ToList(),
        }).ToList();

        // Załaduj Person do User dla mapowania
        user.Person = person;
        return user.ToAuthMeResponse(membershipInfos);
    }

    /// <inheritdoc/>
    public async Task<UserDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Użytkownik {id} nie istnieje.");
        return user.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<UserDetailResponse?> GetByExternalIdAsync(string externalId, CancellationToken ct = default)
    {
        var user = await _users.GetByExternalIdAsync(externalId, "kinde", ct);
        return user?.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<AuthMeResponse?> GetMeAsync(string externalId, CancellationToken ct = default)
    {
        var slim = await _users.GetByExternalIdAsync(externalId, "kinde", ct);
        if (slim is null) return null;

        var user = await _users.GetWithPersonAsync(slim.Id, ct);
        if (user?.Person is null) return null;

        var memberships = await _members.GetByPersonIdAsync(user.Person.Id, ct);
        var membershipInfos = memberships.Select(m => new AuthMembershipInfo
        {
            MemberId         = m.Id,
            OrganizationId   = m.OrganizationId,
            OrganizationName = m.Organization?.Name ?? string.Empty,
            IsActive         = m.IsActive,
            Roles            = m.Roles.Select(r => r.Role.ToString()).ToList(),
        }).ToList();

        return user.ToAuthMeResponse(membershipInfos);
    }

    /// <inheritdoc/>
    public async Task<UserDetailResponse> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Użytkownik {id} nie istnieje.");

        if (user.IsActive == isActive)
            return user.ToDetail();

        user.IsActive = isActive;
        await _users.UpdateAsync(user, ct);

        // Synchronizuj z Kinde (best-effort)
        try
        {
            if (isActive)
                await _kinde.UnsuspendUserAsync(user.ExternalId, ct);
            else
                await _kinde.SuspendUserAsync(user.ExternalId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Nie udało się zsynchronizować stanu konta {UserId} z Kinde.", id);
        }

        return user.ToDetail();
    }

    /// <inheritdoc/>
    public async Task SetPreferredLanguageAsync(string externalId, string language, CancellationToken ct = default)
    {
        var user = await _users.GetByExternalIdAsync(externalId, "kinde", ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, "Użytkownik nie istnieje.");

        if (user.PreferredLanguage == language) return;

        user.PreferredLanguage = language;
        await _users.UpdateAsync(user, ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await _users.DeleteAsync(id, ct);
        if (!deleted)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Użytkownik {id} nie istnieje.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<string> GenerateUniqueProfileCodeAsync(CancellationToken ct, int maxAttempts = 10)
    {
        // Alfabet bez liter/cyfr podatnych na pomyłkę wzrokową (O≈0, I≈1, S≈5, B≈8)
        const string Chars = "ABCDEFGHJKLMNPQRTUVWXYZ2346789";
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var code = string.Create(8, Random.Shared, (span, rng) =>
            {
                for (var i = 0; i < span.Length; i++)
                    span[i] = Chars[rng.Next(Chars.Length)];
            });

            if (!await _users.ProfileCodeExistsAsync(code, ct))
                return code;

            _logger.LogWarning("Kolizja ProfileCode przy próbie {Attempt}.", attempt + 1);
        }

        throw new InvalidOperationException("Nie udało się wygenerować unikalnego ProfileCode po wielu próbach.");
    }
}
