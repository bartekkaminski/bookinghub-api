using BookingHub.Api.Dtos.Discipline;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;

namespace BookingHub.Api.Services;

/// <summary>
/// Implementacja serwisu dyscyplin organizacyjnych.
/// </summary>
public sealed class DisciplineService : IDisciplineService
{
    private readonly IDisciplineRepository _disciplines;
    private readonly IOrganizationRepository _organizations;

    public DisciplineService(
        IDisciplineRepository disciplines,
        IOrganizationRepository organizations)
    {
        _disciplines   = disciplines;
        _organizations = organizations;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DisciplineSummaryResponse>> GetAllAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var disciplines = await _disciplines.GetByOrganizationAsync(organizationId, ct);
        var result = new List<DisciplineSummaryResponse>(disciplines.Count);

        foreach (var discipline in disciplines)
        {
            var rankCount = await _disciplines.CountRanksAsync(discipline.Id, ct);
            result.Add(discipline.ToSummary(rankCount));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<DisciplineDetailResponse> GetByIdAsync(
        Guid organizationId, Guid disciplineId, CancellationToken ct = default)
    {
        var discipline = await GetOwnedOrThrowAsync(organizationId, disciplineId, ct);
        var rankCount = await _disciplines.CountRanksAsync(disciplineId, ct);
        return discipline.ToDetail(rankCount);
    }

    /// <inheritdoc/>
    public async Task<DisciplineDetailResponse> CreateAsync(
        Guid organizationId, CreateDisciplineRequest request, CancellationToken ct = default)
    {
        var orgExists = await _organizations.ExistsAsync(organizationId, ct);
        if (!orgExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {organizationId} nie istnieje.");

        if (await _disciplines.IsNameTakenAsync(organizationId, request.Name.Trim(), null, ct))
            throw new ServiceException(ServiceErrorCode.DisciplineNameTaken,
                $"Dyscyplina o nazwie '{request.Name}' już istnieje w tej organizacji.", nameof(request.Name));

        var entity = new Discipline
        {
            OrganizationId = organizationId,
            Name           = request.Name.Trim(),
            Color          = request.Color?.Trim(),
        };

        var created = await _disciplines.AddAsync(entity, ct);
        return created.ToDetail(0);
    }

    /// <inheritdoc/>
    public async Task<DisciplineDetailResponse> UpdateAsync(
        Guid organizationId, Guid disciplineId, UpdateDisciplineRequest request, CancellationToken ct = default)
    {
        var discipline = await GetOwnedOrThrowAsync(organizationId, disciplineId, ct);

        if (!string.Equals(discipline.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
            await _disciplines.IsNameTakenAsync(discipline.OrganizationId, request.Name.Trim(), excludeId: disciplineId, ct))
            throw new ServiceException(ServiceErrorCode.DisciplineNameTaken,
                $"Dyscyplina o nazwie '{request.Name}' już istnieje w tej organizacji.", nameof(request.Name));

        discipline.ApplyUpdate(request);
        await _disciplines.UpdateAsync(discipline, ct);

        var rankCount = await _disciplines.CountRanksAsync(disciplineId, ct);
        return discipline.ToDetail(rankCount);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid organizationId, Guid disciplineId, CancellationToken ct = default)
    {
        await GetOwnedOrThrowAsync(organizationId, disciplineId, ct);

        var rankCount = await _disciplines.CountRanksAsync(disciplineId, ct);
        if (rankCount > 0)
            throw new ServiceException(ServiceErrorCode.DisciplineHasRanks,
                "Nie można usunąć dyscypliny, która ma zdefiniowane rangi. Usuń najpierw wszystkie rangi.");

        await _disciplines.DeleteAsync(disciplineId, ct);
    }

    /// <summary>
    /// Pobiera dyscyplinę i weryfikuje, że należy do wskazanej organizacji — chroni przed IDOR
    /// (dostępem do/modyfikacją dyscypliny innej organizacji przez odgadnięcie jej Id).
    /// </summary>
    private async Task<Discipline> GetOwnedOrThrowAsync(Guid organizationId, Guid disciplineId, CancellationToken ct)
    {
        var discipline = await _disciplines.GetByIdAsync(disciplineId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Dyscyplina {disciplineId} nie istnieje.");

        if (discipline.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Dyscyplina {disciplineId} nie istnieje w tej organizacji.");

        return discipline;
    }
}
