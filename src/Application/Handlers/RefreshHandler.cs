using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Application.Commands;
using Application.DTOs;
using Application.Interfaces;

namespace Application.Handlers;

public class RefreshHandler : IRequestHandler<RefreshCommand, AuthResponse>
{
    private readonly ITokenService _tokens;

    public RefreshHandler(ITokenService tokens) => _tokens = tokens;

    public async Task<AuthResponse> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var ok = await _tokens.ValidateRefreshTokenAsync(request.Request.RefreshToken);
        if (!ok) throw new ApplicationException("Invalid refresh token");

        var user = await _tokens.GetUserByRefreshTokenAsync(request.Request.RefreshToken);
        if (user is null) throw new ApplicationException("Invalid refresh token - user not found");

        // Revoke the used refresh token to avoid replay
        await _tokens.RevokeRefreshTokenAsync(request.Request.RefreshToken);

        var (access, refresh, expiresAt) = await _tokens.CreateTokensAsync(user);
        return new AuthResponse(access, refresh, expiresAt);
    }
}
