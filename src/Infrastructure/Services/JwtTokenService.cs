using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Settings;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly AppDbContext _db;

    public JwtTokenService(IOptions<JwtSettings> options, AppDbContext db)
    {
        _settings = options.Value;
        _db = db;
    }

    public async Task<(string accessToken, string refreshToken, DateTime expiresAt)> CreateTokensAsync(User user)
    {
        var now = DateTime.UtcNow;
        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var access = new JwtSecurityTokenHandler().WriteToken(token);

        // create refresh token and persist
        var refresh = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Guid.NewGuid();
        var rt = new Domain.Entities.RefreshToken { Token = refresh, ExpiresAt = now.AddDays(_settings.RefreshTokenDays), UserId = user.Id };
        await _db.RefreshTokens.AddAsync(rt);
        await _db.SaveChangesAsync();

        return (access, refresh, expires);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
        if (rt is null) return false;
        if (rt.IsRevoked) return false;
        if (rt.ExpiresAt < DateTime.UtcNow) return false;
        return true;
    }

    public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
    {
        var rt = await _db.RefreshTokens.Include(r => r.User).FirstOrDefaultAsync(x => x.Token == refreshToken);
        return rt?.User;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
        if (rt is null) return;
        rt.IsRevoked = true;
        _db.RefreshTokens.Update(rt);
        await _db.SaveChangesAsync();
    }
}
