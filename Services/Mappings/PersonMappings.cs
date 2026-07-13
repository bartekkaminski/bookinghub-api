using BookingHub.Api.Dtos.Person;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class PersonMappings
{
    public static PersonSummaryResponse ToSummary(this Person person) => new()
    {
        Id          = person.Id,
        FirstName   = person.FirstName,
        LastName    = person.LastName,
        PhotoUrl    = person.PhotoUrl,
        DateOfBirth = person.DateOfBirth,
        HasAccount  = person.UserId is not null,
    };

    public static PersonDetailResponse ToDetail(this Person person,
        IReadOnlyList<PersonSummaryResponse>? children = null,
        IReadOnlyList<PersonSummaryResponse>? parents  = null) => new()
    {
        Id          = person.Id,
        FirstName   = person.FirstName,
        LastName    = person.LastName,
        PhotoUrl    = person.PhotoUrl,
        DateOfBirth = person.DateOfBirth,
        HasAccount  = person.UserId is not null,
        UserId      = person.UserId,
        Email       = person.User?.Email,
        ProfileCode = string.IsNullOrEmpty(person.User?.ProfileCode) ? null : person.User.ProfileCode,
        Memberships = person.Memberships.Select(m => new PersonMembershipInfo
        {
            MemberId         = m.Id,
            OrganizationId   = m.OrganizationId,
            OrganizationName = m.Organization?.Name ?? string.Empty,
            Roles            = m.Roles.Select(r => r.Role.ToString()).ToList(),
            IsActive         = m.IsActive,
        }).ToList(),
        Children    = children ?? [],
        Parents     = parents  ?? [],
        CreatedAt   = person.CreatedAt,
        UpdatedAt   = person.UpdatedAt,
    };

    public static Person ToEntity(this CreatePersonRequest dto) => new()
    {
        FirstName   = dto.FirstName?.Trim(),
        LastName    = dto.LastName?.Trim(),
        DateOfBirth = dto.DateOfBirth,
        PhotoUrl    = dto.PhotoUrl?.Trim(),
    };

    public static void ApplyUpdate(this Person person, UpdatePersonRequest dto)
    {
        person.FirstName   = dto.FirstName?.Trim();
        person.LastName    = dto.LastName?.Trim();
        person.DateOfBirth = dto.DateOfBirth;
        person.PhotoUrl    = dto.PhotoUrl?.Trim();
    }
}
