using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Application.Commands;
using Application.DTOs;
using Domain.Interfaces;
using Application.Interfaces;
using System.Linq;
using Domain.Entities;

namespace Application.Handlers;

public class LoginHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IRepository<User> _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public LoginHandler(IRepository<User> users, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var list = await _users.ListAsync();
        var user = list.FirstOrDefault(u => u.Username == request.Request.UsernameOrEmail || u.Email == request.Request.UsernameOrEmail);
        if (user is null) throw new ApplicationException("Invalid credentials");

        if (!_hasher.Verify(request.Request.Password, user.PasswordHash)) throw new ApplicationException("Invalid credentials");

        var (access, refresh, expiresAt) = await _tokens.CreateTokensAsync(user);
        return new AuthResponse(access, refresh, expiresAt);
    }
}
