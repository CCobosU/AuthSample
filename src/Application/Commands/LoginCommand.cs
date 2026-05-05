using MediatR;
using Application.DTOs;

namespace Application.Commands;

public record LoginCommand(LoginRequest Request) : IRequest<AuthResponse>;
