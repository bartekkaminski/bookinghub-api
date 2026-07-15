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
    private readonly IOrganizationMemberRepository _members;
    private readonly IOrganizationRepository _organizations;
    private readonly AppDbContext _context;
    private readonly ILogger<RankService> _logger;

    public RankService(
        IRankRepository ranks,
        IOrganizationMemberRepository members,
        IOrganizationRepository organizations,
        AppDbContext context,
        ILogger<RankService> logger)
    {
        _ranks         = ranks;
        _members       = members;
        _organizations = organizations;
        _context       = context;
        _logger        = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RankSummaryResponse>> GetAllAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var ranks = await _ranks.GetByOrganizationAsync(organizationId, ct);
        var result = new List<RankSummaryResponse>(ranks.Count);

        foreach (var rank in ranks)
        {
            var count = await _ranks.CountMembersAsync(rank.Id, ct);
            result.Add(rank.ToSummary(count));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<RankDetailResponse> GetByIdAsync(Guid rankId, CancellationToken ct = default)
    {
        var rank = await _ranks.GetByIdAsync(rankId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Ranga {rankId} nie istnieje.");

        var count = await _ranks.CountMembersAsync(rankId, ct);
        return rank.ToDetail(count);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<MemberSummaryResponse>> GetMembersAsync(
        Guid rankId, int page, int pageSize, CancellationToken ct = default)
    {
        var rankExists = await _ranks.ExistsAsync(rankId, ct);
        if (!rankExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Ranga {rankId} nie istnieje.");

        var paged = await _ranks.GetPagedMembersAsync(rankId, page, pageSize, ct);
        return paged.Map(m => m.ToSummary());
    }

    /// <inheritdoc/>
    public async Task<RankDetailResponse> CreateAsync(
        Guid organizationId, CreateRankRequest request, CancellationToken ct = default)
    {
        var orgExists = await _organizations.ExistsAsync(organizationId, ct);
        if (!orgExists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Organizacja {organizationId} nie istnieje.");

        if (await _ranks.IsNameTakenAsync(organizationId, request.Name.Trim(), null, ct))
            throw new ServiceException(ServiceErrorCode.RankNameTaken,
                $"Ranga o nazwie '{request.Name}' już istnieje w tej organizacji.", nameof(request.Name));

        var entity = new OrganizationRank
        {
            OrganizationId = organizationId,
            Name           = request.Name.Trim(),
            Color          = request.Color?.Trim(),
        };

        var created = await _ranks.AddAsync(entity, ct);
        return created.ToDetail(0);
    }

    /// <inheritdoc/>
    public async Task<RankDetailResponse> UpdateAsync(
        Guid rankId, UpdateRankRequest request, CancellationToken ct = default)
    {
        var rank = await _ranks.GetByIdAsync(rankId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Ranga {rankId} nie istnieje.");

        if (!string.Equals(rank.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
            await _ranks.IsNameTakenAsync(rank.OrganizationId, request.Name.Trim(), excludeId: rankId, ct))
            throw new ServiceException(ServiceErrorCode.RankNameTaken,
                $"Ranga o nazwie '{request.Name}' już istnieje w tej organizacji.", nameof(request.Name));

        rank.ApplyUpdate(request);
        await _ranks.UpdateAsync(rank, ct);

        var count = await _ranks.CountMembersAsync(rankId, ct);
        return rank.ToDetail(count);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid rankId, CancellationToken ct = default)
    {
        var exists = await _ranks.ExistsAsync(rankId, ct);
        if (!exists)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Ranga {rankId} nie istnieje.");

        await _ranks.DeleteAsync(rankId, ct);
    }

    /// <inheritdoc/>
    public async Task<MemberDetailResponse> SetMemberRankAsync(
        Guid organizationId, Guid memberId, Guid? rankId, CancellationToken ct = default)
    {
        // Pobierz członka z trackowaniem (potrzebna aktualizacja)
        var member = await _context.Set<OrganizationMember>()
            .Include(m => m.Person)
            .Include(m => m.Roles)
            .Include(m => m.GroupMemberships).ThenInclude(gm => gm.Group)
            .Include(m => m.TeamMemberships).ThenInclude(tm => tm.Team)
            .Include(m => m.AssignedTrainers).ThenInclude(pt => pt.Trainer).ThenInclude(t => t!.Person)
            .Include(m => m.Rank)
            .FirstOrDefaultAsync(m => m.Id == memberId, ct)
            ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Członek {memberId} nie istnieje.");

        if (member.OrganizationId != organizationId)
            throw new ServiceException(ServiceErrorCode.NotFound, $"Członek {memberId} nie istnieje w tej organizacji.");

        if (rankId.HasValue)
        {
            var rank = await _ranks.GetByIdAsync(rankId.Value, ct)
                ?? throw new ServiceException(ServiceErrorCode.NotFound, $"Ranga {rankId} nie istnieje.");

            if (rank.OrganizationId != organizationId)
                throw new ServiceException(ServiceErrorCode.ValidationError,
                    "Ranga należy do innej organizacji niż członek.");

            member.RankId = rankId.Value;
            member.Rank   = rank;
        }
        else
        {
            member.RankId = null;
            member.Rank   = null;
        }

        await _context.SaveChangesAsync(ct);
        return member.ToDetail();
    }
}
