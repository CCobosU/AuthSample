using MediatR;
using Application.DTOs;

namespace Application.Commands;

public record RegisterCommand(RegisterRequest Request) : IRequest<AuthResponse>;
