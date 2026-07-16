using BookingHub.Api.Data;
using BookingHub.Api.Dtos.Member;
using BookingHub.Api.Dtos.Rank;
using BookingHub.Api.Models;
using BookingHub.Api.Repositories.Common;
using BookingHub.Api.Repositories.Interfaces;
using BookingHub.Api.Services.Exceptions;
using BookingHub.Api.Services.Interfaces;
using BookingHub.Api.Services.Mappings;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Api.Services;

/// <summary>
/// Implementacja serwisu rang organizacyjnych.
/// </summary>
public sealed class RankService : IRankService
{
    private readonly IRankRepository _ranks;
    private readonly IDisciplineRepository _disciplines;
    private readonly AppDbContext _context;
    private readonly ILogger<RankService> _logger;

    public RankService(
        IRankRepository ranks,
        IDisciplineRepository disciplines,
        AppDbContext context,
        ILogger<RankService> logger)
    {
        _ranks       = ranks;
        _disciplines = disciplines;
        _context     = context;
        _logger      = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RankSummaryResponse>> GetAllAsync(
        Guid organizationId, Guid disciplineId, CancellationToken ct = default)
    {
        var discipline = await GetOwnedDisciplineOrThrowAsync(organizationId, disciplineId, ct);

        var ranks = await _ranks.GetByDisciplineAsync(disciplineId, ct);
        var result = new List<RankSummaryResponse>(ranks.Count);

        foreach (var rank in ranks)
        {
            var count = await _ranks.CountMembersAsync(rank.Id, ct);
            result.Add(rank.ToSummary(discipline.Name, count));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<RankDetailResponse> GetByIdAsync(
        Guid organizationId, Guid disciplineId, Guid rankId, CancellationToken ct = default)
    {
        var rank = await GetOwnedRankOrThrowAsync(organizationId, disciplineId, rankId, ct);

        var discipline = await _disciplines.GetByIdAsync(rank.DisciplineId, ct);
        var count = await _ranks.CountMembersAsync(rankId, ct);
        return rank.ToDetail(discipline?.Name ?? string.Empty, count);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MemberSummaryResponse>> GetMembersAsync(
        Guid organizationId, Guid disciplineId, Guid rankId, int page, int pageSize, CancellationToken ct = default)
    {
        await GetOwnedRankOrThrowAsync(organizationId, disciplineId, rankId, ct);

        var paged = await _ranks.GetPagedMembersAsync(rankId, page, pageSize, ct);
        return paged.Map(m => m.ToSummary());
    }

    /// <inheritdoc/>
    public async Task<RankDetailResponse> CreateAsync(
        Guid organizationId, Guid disciplineId, CreateRankRequest request, CancellationToken ct = default)
    {
        var discipline = await GetOwnedDisciplineOrThrowAsync(organizationId, disciplineId, ct);

        if (await _ranks.IsNameTakenAsync(disciplineId, request.Name.Trim(), null, ct))
            throw new ServiceException(ServiceErrorCode.RankNameTaken,
                $"Ranga o nazwie '{request.Name}' już istnieje w tej dyscyplinie.", nameof(request.Name));

        var entity = new OrganizationRank
        {
            OrganizationId = discipline.OrganizationId,
            DisciplineId   = disciplineId,
            Name           = request.Name.Trim(),
            Color          = request.Color?.Trim(),
        };

        var created = await _ranks.AddAsync(entity, ct);
        return created.ToDetail(discipline.Name, 0);
    }

    /// <inheritdoc/>
    public async Task<RankDetailResponse> UpdateAsync(
        Guid organizationId, Guid disciplineId, Guid rankId, UpdateRankRequest request, CancellationToken ct = default)
    {
        var rank = await GetOwnedRankOrThrowAsync(organizationId, disciplineId, rankId, ct);

        if (!string.Equals(rank.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
            await _ranks.IsNameTakenAsync(rank.DisciplineId, request.Name.Trim(), excludeId: rankId, ct))
            throw new ServiceException(ServiceErrorCode.RankNameTaken,
                $"Ranga o nazwie '{request.Name}' już istnieje w tej dyscyplinie.", nameof(request.Name));

        rank.ApplyUpdate(request);
        await _ranks.UpdateAsync(rank, ct);

        var discipline = await _disciplines.GetByIdAsync(rank.DisciplineId, ct);
        var count = await _ranks.CountMembersAsync(rankId, ct);
        return rank.ToDetail(discipline?.Name ?? string.Empty, count);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid organizationId, Guid disciplineId, Guid rankId, CancellationToken ct = default)
    {
        await GetOwnedRankOrThrowAsync(organizationId, disciplineId, rankId, ct);

        // Przypisania członków (MemberRank) do tej rangi są usuwane kaskadowo przez AppDbContext.
        await _ranks.DeleteAsync(rankId, ct);
    }

    /// <inheritdoc/>
    public async Task<MemberDetailResponse> SetMemberRankAsync(
        Guid organizationId, Guid memberId, Guid disciplineId, Guid? rankId, CancellationToken ct = default)
    {
        var memberExists = await _context.Set<OrganizationMember>()
            .AnyAsync(m => m.Id == memberId && m.OrganizationId == organizationId, ct);
        if (!memberExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Członek {memberId} nie istnieje w tej organizacji.");

        var discipline = await _disciplines.GetByIdAsync(disciplineId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Dyscyplina {disciplineId} nie istnieje.");
        if (discipline.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.ValidationError,
                "Dyscyplina należy do innej organizacji niż członek.");

        var existing = await _context.Set<MemberRank>()
            .FirstOrDefaultAsync(mr => mr.MemberId == memberId && mr.DisciplineId == disciplineId, ct);

        if (rankId.HasValue)
        {
            var rank = await _ranks.GetByIdAsync(rankId.Value, ct)
                ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Ranga {rankId} nie istnieje.");

            if (rank.OrganizationId != organizationId)
                throw new ServiceException(ServiceErrorCode.ValidationError,
                    "Ranga należy do innej organizacji niż członek.");

            if (rank.DisciplineId != disciplineId)
                throw new ServiceException(ServiceErrorCode.ValidationError,
                    "Ranga nie należy do wskazanej dyscypliny.");

            if (existing is not null)
            {
                existing.RankId = rankId.Value;
            }
            else
            {
                await _context.Set<MemberRank>().AddAsync(new MemberRank
                {
                    MemberId     = memberId,
                    DisciplineId = disciplineId,
                    RankId       = rankId.Value,
                }, ct);
            }
        }
        else if (existing is not null)
        {
            _context.Set<MemberRank>().Remove(existing);
        }

        await _context.SaveChangesAsync(ct);

        var member = await _context.Set<OrganizationMember>()
            .Include(m => m.Person)
            .Include(m => m.Roles)
            .Include(m => m.GroupMemberships).ThenInclude(gm => gm.Group)
            .Include(m => m.TeamMemberships).ThenInclude(tm => tm.Team)
            .Include(m => m.AssignedTrainers).ThenInclude(pt => pt.Trainer).ThenInclude(t => t!.Person)
            .Include(m => m.MemberRanks).ThenInclude(mr => mr.Discipline)
            .Include(m => m.MemberRanks).ThenInclude(mr => mr.Rank)
            .FirstAsync(m => m.Id == memberId, ct);

        return member.ToDetail();
    }

    /// <summary>
    /// Pobiera dyscyplinę i weryfikuje, że należy do wskazanej organizacji — chroni przed IDOR
    /// (odwołaniem się do/tworzeniem rang w dyscyplinie innej organizacji przez odgadnięcie jej Id).
    /// </summary>
    private async Task<Discipline> GetOwnedDisciplineOrThrowAsync(
        Guid organizationId, Guid disciplineId, CancellationToken ct)
    {
        var discipline = await _disciplines.GetByIdAsync(disciplineId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Dyscyplina {disciplineId} nie istnieje.");

        if (discipline.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Dyscyplina {disciplineId} nie istnieje w tej organizacji.");

        return discipline;
    }

    /// <summary>
    /// Pobiera rangę i weryfikuje, że należy do wskazanej organizacji i dyscypliny — chroni przed
    /// IDOR (dostępem do/modyfikacją rangi innej organizacji lub innej dyscypliny przez odgadnięcie jej Id).
    /// </summary>
    private async Task<OrganizationRank> GetOwnedRankOrThrowAsync(
        Guid organizationId, Guid disciplineId, Guid rankId, CancellationToken ct)
    {
        var rank = await _ranks.GetByIdAsync(rankId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Ranga {rankId} nie istnieje.");

        if (rank.OrganizationId != organizationId || rank.DisciplineId != disciplineId)
            throw new ServiceException(ServiceErrorCode.NotFound,
                $"Ranga {rankId} nie istnieje w tej dyscyplinie.");

        return rank;
    }
}
