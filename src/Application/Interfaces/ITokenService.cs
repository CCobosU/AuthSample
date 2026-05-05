using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces;

public interface ITokenService
{
    Task<(string accessToken, string refreshToken, System.DateTime expiresAt)> CreateTokensAsync(User user);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken);
    Task<User?> GetUserByRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
}
