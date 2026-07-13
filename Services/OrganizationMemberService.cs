using BookingHub.Api.Dtos.Member;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania członkostwami w organizacjach i rolami.
/// </summary>
public sealed class OrganizationMemberService : IOrganizationMemberService
{
    private readonly IOrganizationMemberRepository _members;
    private readonly IOrganizationRepository _organizations;
    private readonly IPersonRepository _persons;
    private readonly IKindeManagementService _kinde;
    private readonly IUserRepository _users;
    private readonly ILogger<OrganizationMemberService> _logger;

    public OrganizationMemberService(
        IOrganizationMemberRepository members,
        IOrganizationRepository organizations,
        IPersonRepository persons,
        IKindeManagementService kinde,
        IUserRepository users,
        ILogger<OrganizationMemberService> logger)
    {
        _members       = members;
        _organizations = organizations;
        _persons       = persons;
        _kinde         = kinde;
        _users         = users;
        _logger        = logger;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MemberSummaryResponse>> GetPagedAsync(Guid organizationId, OrganizationMemberFilterParams filter, CancellationToken ct = default)
    {
        filter.OrganizationId = organizationId;
        var paged = await _members.GetPagedAsync(filter, ct);
        return paged.Map(m => m.ToSummary());
    }

    /// <inheritdoc/>
    public async Task<MemberDetailResponse> GetByIdAsync(Guid memberId, CancellationToken ct = default)
    {
        var member = await _members.GetWithDetailsAsync(memberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Członek {memberId} nie istnieje.");
        return member.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MemberSummaryResponse>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var members = await _members.GetByOrganizationAsync(organizationId, ct);
        return members.Select(m => m.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MemberSummaryResponse>> GetTrainersAsync(Guid organizationId, CancellationToken ct = default)
    {
        var trainers = await _members.GetByRoleAsync(organizationId, MemberRole.Trainer, ct);
        return trainers.Select(m => m.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MemberSummaryResponse>> GetParticipantsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var participants = await _members.GetByRoleAsync(organizationId, MemberRole.Participant, ct);
        return participants.Select(m => m.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<MemberDetailResponse> AddMemberAsync(Guid organizationId, AddMemberRequest request, CancellationToken ct = default)
    {
        var orgExists = await _organizations.ExistsAsync(organizationId, ct);
        if (!orgExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {organizationId} nie istnieje.");

        var person = await _persons.GetByIdAsync(request.PersonId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Osoba {request.PersonId} nie istnieje.");

        var alreadyMember = await _members.IsMemberAsync(request.PersonId, organizationId, ct);
        if (alreadyMember)
            throw new ServiceException(ServiceErrorCode.AlreadyMember,
                "Ta osoba jest już członkiem tej organizacji.");

        if (!request.Roles.Any())
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Co najmniej jedna rola jest wymagana.", nameof(request.Roles));

        var member = new OrganizationMember
        {
            OrganizationId = organizationId,
            PersonId       = request.PersonId,
            DisplayName    = request.DisplayName?.Trim(),
            Color          = request.Color?.Trim(),
            Priority       = request.Priority,
            IsActive       = true,
            Roles          = request.Roles.Select(r => new OrganizationMemberRole { Role = r }).ToList(),
        };

        var created = await _members.AddAsync(member, ct);
        var withDetails = await _members.GetWithDetailsAsync(created.Id, ct);
        return withDetails!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<MemberDetailResponse> CreateMemberWithAccountAsync(Guid organizationId, CreateMemberWithAccountRequest request, CancellationToken ct = default)
    {
        var orgExists = await _organizations.ExistsAsync(organizationId, ct);
        if (!orgExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {organizationId} nie istnieje.");

        if (await _users.IsEmailTakenAsync(request.Email, null, ct))
            throw new ServiceException(ServiceErrorCode.EmailAlreadyTaken,
                $"Adres e-mail '{request.Email}' jest już zajęty.", nameof(request.Email));

        // 1. Utwórz konto w Kinde → pobierz ExternalId
        var externalId = await _kinde.CreateUserInKindeAsync(request.FirstName, request.LastName, request.Email, ct);

        try
        {
            // 2. Utwórz User
            var user = new User
            {
                ExternalId   = externalId,
                AuthProvider = "kinde",
                Email        = request.Email.Trim().ToLowerInvariant(),
                IsActive     = true,
            };
            user = await _users.AddAsync(user, ct);

            // 3. Utwórz Person
            var person = new Person
            {
                UserId      = user.Id,
                FirstName   = request.FirstName.Trim(),
                LastName    = request.LastName.Trim(),
                DateOfBirth = request.DateOfBirth,
            };
            person = await _persons.AddAsync(person, ct);

            // 4. Utwórz OrganizationMember
            var member = new OrganizationMember
            {
                OrganizationId = organizationId,
                PersonId       = person.Id,
                DisplayName    = request.DisplayName?.Trim(),
                Color          = request.Color?.Trim(),
                Priority       = request.Priority,
                IsActive       = true,
                Roles          = request.Roles.Select(r => new OrganizationMemberRole { Role = r }).ToList(),
            };
            member = await _members.AddAsync(member, ct);
            var withDetails = await _members.GetWithDetailsAsync(member.Id, ct);
            return withDetails!.ToDetail();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "Użytkownik {KindeId} ({Email}) został utworzony w Kinde, ale wystąpił błąd zapisu do DB. " +
                "Provisioning zsynchronizuje dane przy pierwszym logowaniu.",
                externalId, request.Email);
            throw new ServiceException(ServiceErrorCode.DatabaseError,
                "Konto zostało utworzone w Kinde, ale wystąpił błąd zapisu do bazy danych.");
        }
    }

    /// <inheritdoc/>
    public async Task<MemberDetailResponse> CreateMemberProfileAsync(Guid organizationId, CreateMemberProfileRequest request, CancellationToken ct = default)
    {
        var orgExists = await _organizations.ExistsAsync(organizationId, ct);
        if (!orgExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {organizationId} nie istnieje.");

        if (!request.Roles.Any())
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Co najmniej jedna rola jest wymagana.", nameof(request.Roles));

        // Profil bez konta ma sens tylko dla Uczestnika
        if (request.Roles.Any(r => r != MemberRole.Participant))
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Profil bez konta logowania może mieć wyłącznie rolę Uczestnik.", nameof(request.Roles));

        // Utwórz Person bez User (bez konta logowania)
        var person = new Person
        {
            UserId      = null,
            FirstName   = request.FirstName.Trim(),
            LastName    = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth,
        };
        person = await _persons.AddAsync(person, ct);

        // Utwórz OrganizationMember
        var member = new OrganizationMember
        {
            OrganizationId = organizationId,
            PersonId       = person.Id,
            DisplayName    = request.DisplayName?.Trim(),
            Color          = request.Color?.Trim(),
            Priority       = request.Priority,
            IsActive       = true,
            Roles          = request.Roles.Select(r => new OrganizationMemberRole { Role = r }).ToList(),
        };
        member = await _members.AddAsync(member, ct);

        _logger.LogInformation(
            "Utworzono profil bez konta PersonId={PersonId} jako członek org {OrgId}.",
            person.Id, organizationId);

        var withDetails = await _members.GetWithDetailsAsync(member.Id, ct);
        return withDetails!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<MemberDetailResponse> UpdateAsync(Guid memberId, UpdateMemberRequest request, CancellationToken ct = default)
    {
        var member = await _members.GetByIdAsync(memberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Członek {memberId} nie istnieje.");

        member.ApplyUpdate(request);
        await _members.UpdateAsync(member, ct);

        // Aktualizuj dane osobowe (Person) jeśli podano
        if (request.FirstName is not null || request.LastName is not null || request.DateOfBirth.HasValue)
        {
            var person = await _persons.GetByIdAsync(member.PersonId, ct)
                ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Profil osoby {member.PersonId} nie istnieje.");

            person.ApplyPersonUpdate(request);
            await _persons.UpdateAsync(person, ct);
        }

        var withDetails = await _members.GetWithDetailsAsync(memberId, ct);
        return withDetails!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<MemberDetailResponse> SetActiveAsync(Guid memberId, bool isActive, CancellationToken ct = default)
    {
        var member = await _members.GetByIdAsync(memberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Członek {memberId} nie istnieje.");

        member.IsActive = isActive;
        await _members.UpdateAsync(member, ct);

        var withDetails = await _members.GetWithDetailsAsync(memberId, ct);
        return withDetails!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<MemberDetailResponse> AddRoleAsync(Guid memberId, MemberRole role, CancellationToken ct = default)
    {
        var member = await _members.GetWithDetailsAsync(memberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Członek {memberId} nie istnieje.");

        if (member.Roles.Any(r => r.Role == role))
            throw new ServiceException(ServiceErrorCode.RoleAlreadyAssigned,
                $"Członek ma już rolę {role}.");

        await _members.AddRoleDirectAsync(memberId, role, ct);

        var refreshed = await _members.GetWithDetailsAsync(memberId, ct);
        return refreshed!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<MemberDetailResponse> RemoveRoleAsync(Guid memberId, MemberRole role, CancellationToken ct = default)
    {
        var member = await _members.GetWithDetailsAsync(memberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Członek {memberId} nie istnieje.");

        if (role == MemberRole.Admin)
        {
            var adminCount = await _members.CountAdminsInOrgAsync(member.OrganizationId, ct);
            if (adminCount <= 1)
                throw new ServiceException(ServiceErrorCode.CannotRemoveLastAdmin,
                    "Nie można usunąć ostatniego Admina organizacji.");
        }

        if (!member.Roles.Any(r => r.Role == role))
            throw new ServiceException(ServiceErrorCode.NotFound, $"Członek nie ma roli {role}.");

        await _members.RemoveRoleDirectAsync(memberId, role, ct);

        var refreshed = await _members.GetWithDetailsAsync(memberId, ct);
        return refreshed!.ToDetail();
    }

    /// <inheritdoc/>
    public async Task AssignTrainerToParticipantAsync(Guid participantMemberId, Guid trainerMemberId, CancellationToken ct = default)
    {
        var participant = await _members.GetWithDetailsAsync(participantMemberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Uczestnik {participantMemberId} nie istnieje.");

        var trainer = await _members.GetWithDetailsAsync(trainerMemberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Trener {trainerMemberId} nie istnieje.");

        // Trener i uczestnik muszą być w tej samej organizacji.
        if (trainer.OrganizationId != participant.OrganizationId)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Trener i uczestnik muszą należeć do tej samej organizacji.");

        if (!trainer.Roles.Any(r => r.Role == MemberRole.Trainer))
            throw new ServiceException(ServiceErrorCode.NotATrainer,
                "Wskazana osoba nie ma roli Trenera.");

        if (participant.AssignedTrainers.Any(pt => pt.TrainerMemberId == trainerMemberId))
            throw new ServiceException(ServiceErrorCode.TrainerAlreadyAssignedToParticipant,
                "Trener jest już przypisany do tego uczestnika.");

        await _members.AddParticipantTrainerAsync(participantMemberId, trainerMemberId, ct);
    }

    /// <inheritdoc/>
    public async Task RemoveTrainerFromParticipantAsync(Guid participantMemberId, Guid trainerMemberId, CancellationToken ct = default)
    {
        var exists = await _members.ParticipantTrainerExistsAsync(participantMemberId, trainerMemberId, ct);
        if (!exists)
            throw new ServiceException(ServiceErrorCode.NotFound,
                "Trener nie jest przypisany do tego uczestnika.");

        await _members.RemoveParticipantTrainerAsync(participantMemberId, trainerMemberId, ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid memberId, CancellationToken ct = default)
    {
        var deleted = await _members.DeleteAsync(memberId, ct);
        if (!deleted)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Członek {memberId} nie istnieje.");
    }

    /// <inheritdoc/>
    public async Task<MemberLookupResponse> FindByCodeAsync(Guid organizationId, string profileCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(profileCode))
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Kod profilu jest wymagany.", nameof(profileCode));

        var user = await _users.GetByProfileCodeAsync(profileCode.Trim().ToUpperInvariant(), ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound,
                "Nie znaleziono osoby z podanym kodem profilu.");

        if (user.Person is null)
            throw new ServiceException(ServiceErrorCode.NotFound,
                "Nie znaleziono osoby z podanym kodem profilu.");

        var isAlreadyMember = await _members.IsMemberAsync(user.Person.Id, organizationId, ct);

        var fullName = $"{user.Person.FirstName} {user.Person.LastName}".Trim();

        return new MemberLookupResponse
        {
            PersonId       = user.Person.Id,
            FullName       = string.IsNullOrEmpty(fullName) ? "—" : fullName,
            IsAlreadyMember = isAlreadyMember,
        };
    }
}
