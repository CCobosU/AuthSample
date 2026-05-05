using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Domain.Interfaces;
using Application.Commands;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using AutoMapper;
using System.Linq;

namespace Application.Handlers;

public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IRepository<User> _users;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public RegisterHandler(IRepository<User> users, IUnitOfWork uow, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _uow = uow;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Basic uniqueness checks
        var existing = (await _users.ListAsync()).FirstOrDefault(u => u.Username == request.Request.Username || u.Email == request.Request.Email);
        if (existing is not null) throw new ApplicationException("User with same username or email exists");

        var user = new User
        {
            Username = request.Request.Username,
            Email = request.Request.Email,
            PasswordHash = _hasher.Hash(request.Request.Password),
        };

        await _users.AddAsync(user);
        await _uow.SaveChangesAsync();

        var (access, refresh, expiresAt) = await _tokens.CreateTokensAsync(user);
        return new AuthResponse(access, refresh, expiresAt);
    }
}
