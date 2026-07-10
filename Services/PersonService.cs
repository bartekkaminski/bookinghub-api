using BookingHub.Api.Dtos.Person;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Serwis zarządzania profilami osób (Person).
/// Person istnieje niezależnie od konta logowania — dziecko może nie mieć konta.
/// </summary>
public sealed class PersonService : IPersonService
{
    private readonly IPersonRepository _persons;
    private readonly IParentChildRelationRepository _relations;
    private readonly ILogger<PersonService> _logger;

    public PersonService(
        IPersonRepository persons,
        IParentChildRelationRepository relations,
        ILogger<PersonService> logger)
    {
        _persons   = persons;
        _relations = relations;
        _logger    = logger;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<PersonSummaryResponse>> GetPagedAsync(PersonFilterParams filter, CancellationToken ct = default)
    {
        var paged = await _persons.GetPagedAsync(filter, ct);
        return paged.Map(p => p.ToSummary());
    }

    /// <inheritdoc/>
    public async Task<PersonDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var person = await _persons.GetWithDetailsAsync(id, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Osoba {id} nie istnieje.");

        var children = await GetChildrenAsync(id, ct);
        var parents  = await GetParentsAsync(id, ct);
        return person.ToDetail(children, parents);
    }

    /// <inheritdoc/>
    public async Task<PersonDetailResponse?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var person = await _persons.GetByUserIdAsync(userId, ct);
        if (person is null) return null;

        var children = await GetChildrenAsync(person.Id, ct);
        var parents  = await GetParentsAsync(person.Id, ct);
        return person.ToDetail(children, parents);
    }

    /// <inheritdoc/>
    public async Task<PersonDetailResponse> CreateAsync(CreatePersonRequest request, CancellationToken ct = default)
    {
        var entity  = request.ToEntity();
        var created = await _persons.AddAsync(entity, ct);
        return created.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<PersonDetailResponse> CreateForUserAsync(Guid userId, CreatePersonRequest request, CancellationToken ct = default)
    {
        var existing = await _persons.GetByUserIdAsync(userId, ct);
        if (existing is not null)
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Użytkownik ma już powiązany profil osoby.");

        var entity    = request.ToEntity();
        entity.UserId = userId;

        var created = await _persons.AddAsync(entity, ct);
        return created.ToDetail();
    }

    /// <inheritdoc/>
    public async Task<PersonDetailResponse> UpdateAsync(Guid id, UpdatePersonRequest request, CancellationToken ct = default)
    {
        var person = await _persons.GetByIdAsync(id, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Osoba {id} nie istnieje.");

        person.ApplyUpdate(request);
        await _persons.UpdateAsync(person, ct);

        var children = await GetChildrenAsync(id, ct);
        var parents  = await GetParentsAsync(id, ct);
        return person.ToDetail(children, parents);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var person = await _persons.GetByIdAsync(id, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Osoba {id} nie istnieje.");

        if (person.Memberships.Any(m => m.IsActive))
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Nie można usunąć osoby z aktywnymi członkostwami.");

        await _persons.DeleteAsync(id, ct);
    }

    /// <inheritdoc/>
    public async Task AddParentChildRelationAsync(Guid parentPersonId, Guid childPersonId, CancellationToken ct = default)
    {
        if (parentPersonId == childPersonId)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Osoba nie może być swoim własnym rodzicem.", nameof(childPersonId));

        var parentExists = await _persons.ExistsAsync(parentPersonId, ct);
        if (!parentExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Osoba (rodzic) {parentPersonId} nie istnieje.");

        var childExists = await _persons.ExistsAsync(childPersonId, ct);
        if (!childExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Osoba (dziecko) {childPersonId} nie istnieje.");

        var alreadyExists = await _relations.RelationExistsAsync(parentPersonId, childPersonId, ct);
        if (alreadyExists)
            throw new ServiceException(ServiceErrorCode.Conflict,
                "Relacja rodzic–dziecko już istnieje.");

        var relation = new ParentChildRelation
        {
            ParentPersonId = parentPersonId,
            ChildPersonId  = childPersonId,
        };
        await _relations.AddAsync(relation, ct);
    }

    /// <inheritdoc/>
    public async Task RemoveParentChildRelationAsync(Guid parentPersonId, Guid childPersonId, CancellationToken ct = default)
    {
        var relation = await _relations.GetByParentAndChildAsync(parentPersonId, childPersonId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound,
                "Relacja rodzic–dziecko nie istnieje.");

        await _relations.DeleteAsync(relation.Id, ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PersonSummaryResponse>> GetChildrenAsync(Guid personId, CancellationToken ct = default)
    {
        var children = await _persons.GetChildrenAsync(personId, ct);
        return children.Select(c => c.ToSummary()).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PersonSummaryResponse>> GetParentsAsync(Guid personId, CancellationToken ct = default)
    {
        var parents = await _persons.GetParentsAsync(personId, ct);
        return parents.Select(p => p.ToSummary()).ToList();
    }
}
