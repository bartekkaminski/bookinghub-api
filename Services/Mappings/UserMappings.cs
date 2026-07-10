using BookingHub.Api.Dtos.Auth;
using BookingHub.Api.Dtos.User;
using BookingHub.Api.Models;

namespace BookingHub.Api.Services.Mappings;

internal static class UserMappings
{
    public static UserSummaryResponse ToSummary(this User user) => new()
    {
        Id        = user.Id,
        Email     = user.Email,
        IsActive  = user.IsActive,
        CreatedAt = user.CreatedAt,
    };

    public static UserDetailResponse ToDetail(this User user) => new()
    {
        Id           = user.Id,
        ExternalId   = user.ExternalId,
        AuthProvider = user.AuthProvider,
        Email        = user.Email,
        IsActive     = user.IsActive,
        PersonId     = user.Person?.Id,
        CreatedAt    = user.CreatedAt,
        UpdatedAt    = user.UpdatedAt,
    };

    public static AuthMeResponse ToAuthMeResponse(this User user, IReadOnlyList<AuthMembershipInfo> memberships) => new()
    {
        UserId            = user.Id,
        PersonId          = user.Person?.Id ?? Guid.Empty,
        Email             = user.Email,
        FirstName         = user.Person?.FirstName,
        LastName          = user.Person?.LastName,
        PhotoUrl          = user.Person?.PhotoUrl,
        IsActive          = user.IsActive,
        PreferredLanguage = user.PreferredLanguage,
        Memberships       = memberships,
    };
}
