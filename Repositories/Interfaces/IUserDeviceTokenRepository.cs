using BookingHub.Api.Dtos.DeviceToken;
using BookingHub.Api.Models;

namespace BookingHub.Api.Repositories.Interfaces;

public interface IUserDeviceTokenRepository
{
    Task<IReadOnlyList<UserDeviceToken>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserDeviceToken> AddAsync(UserDeviceToken token, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, string token, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, string token, CancellationToken ct = default);
}
